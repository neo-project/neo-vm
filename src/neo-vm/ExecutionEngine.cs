using Neo.VM.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using VMArray = Neo.VM.Types.Array;

namespace Neo.VM
{
    public class ExecutionEngine : IDisposable
    {
        private readonly IScriptTable table;
        private readonly InteropService service;

        public IScriptContainer ScriptContainer { get; }
        public ICrypto Crypto { get; }
        public RandomAccessStack<ExecutionContext> InvocationStack { get; } = new RandomAccessStack<ExecutionContext>();
        public RandomAccessStack<StackItem> EvaluationStack { get; } = new RandomAccessStack<StackItem>();
        public RandomAccessStack<StackItem> AltStack { get; } = new RandomAccessStack<StackItem>();
        public ExecutionContext CurrentContext => InvocationStack.Peek();
        public ExecutionContext CallingContext => InvocationStack.Count > 1 ? InvocationStack.Peek(1) : null;
        public ExecutionContext EntryContext => InvocationStack.Peek(InvocationStack.Count - 1);
        public VMState State { get; protected set; } = VMState.BREAK;

        public ExecutionEngine(IScriptContainer container, ICrypto crypto, IScriptTable table = null, InteropService service = null)
        {
            this.ScriptContainer = container;
            this.Crypto = crypto;
            this.table = table;
            this.service = service ?? new InteropService();
        }

        public void AddBreakPoint(uint position)
        {
            CurrentContext.BreakPoints.Add(position);
        }

        public void Dispose()
        {
            while (InvocationStack.Count > 0)
                InvocationStack.Pop().Dispose();
        }

        public void Execute()
        {
            State &= ~VMState.BREAK;
            while (!State.HasFlag(VMState.HALT) && !State.HasFlag(VMState.FAULT) && !State.HasFlag(VMState.BREAK))
                StepInto();
        }

        private void ExecuteOp(OpCode opcode, ExecutionContext context)
        {
            if (opcode > OpCode.PUSH16 && opcode != OpCode.RET && context.PushOnly)
            {
                State |= VMState.FAULT;
                return;
            }
            if (opcode >= OpCode.PUSHBYTES1 && opcode <= OpCode.PUSHBYTES75)
                EvaluationStack.Push(context.OpReader.ReadBytes((byte)opcode));
            else
                switch (opcode)
                {
                    // Push value
                    case OpCode.PUSH0:
                        EvaluationStack.Push(new byte[0]);
                        break;
                    case OpCode.PUSHDATA1:
                        EvaluationStack.Push(context.OpReader.ReadBytes(context.OpReader.ReadByte()));
                        break;
                    case OpCode.PUSHDATA2:
                        EvaluationStack.Push(context.OpReader.ReadBytes(context.OpReader.ReadUInt16()));
                        break;
                    case OpCode.PUSHDATA4:
                        EvaluationStack.Push(context.OpReader.ReadBytes(context.OpReader.ReadInt32()));
                        break;
                    case OpCode.PUSHM1:
                    case OpCode.PUSH1:
                    case OpCode.PUSH2:
                    case OpCode.PUSH3:
                    case OpCode.PUSH4:
                    case OpCode.PUSH5:
                    case OpCode.PUSH6:
                    case OpCode.PUSH7:
                    case OpCode.PUSH8:
                    case OpCode.PUSH9:
                    case OpCode.PUSH10:
                    case OpCode.PUSH11:
                    case OpCode.PUSH12:
                    case OpCode.PUSH13:
                    case OpCode.PUSH14:
                    case OpCode.PUSH15:
                    case OpCode.PUSH16:
                        EvaluationStack.Push((int)opcode - (int)OpCode.PUSH1 + 1);
                        break;

                    // Control
                    case OpCode.NOP:
                        break;
                    case OpCode.JMP:
                    case OpCode.JMPIF:
                    case OpCode.JMPIFNOT:
                        {
                            int offset = context.OpReader.ReadInt16();
                            offset = context.InstructionPointer + offset - 3;
                            if (offset < 0 || offset > context.Script.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool fValue = true;
                            if (opcode > OpCode.JMP)
                            {
                                fValue = EvaluationStack.Pop().GetBoolean();
                                if (opcode == OpCode.JMPIFNOT)
                                    fValue = !fValue;
                            }
                            if (fValue)
                                context.InstructionPointer = offset;
                        }
                        break;
                    case OpCode.CALL:
                        InvocationStack.Push(context.Clone());
                        context.InstructionPointer += 2;
                        ExecuteOp(OpCode.JMP, CurrentContext);
                        break;
                    case OpCode.RET:
                        InvocationStack.Pop().Dispose();
                        if (InvocationStack.Count == 0)
                            State |= VMState.HALT;
                        break;
                    case OpCode.APPCALL:
                    case OpCode.TAILCALL:
                        {
                            if (table == null)
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            byte[] script_hash = context.OpReader.ReadBytes(20);
                            if (script_hash.All(p => p == 0))
                            {
                                script_hash = EvaluationStack.Pop().GetByteArray();
                            }

                            byte[] script = table.GetScript(script_hash);
                            if (script == null)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            if (opcode == OpCode.TAILCALL)
                                InvocationStack.Pop().Dispose();
                            LoadScript(script);
                        }
                        break;
                    case OpCode.SYSCALL:
                        if (!service.Invoke(Encoding.ASCII.GetString(context.OpReader.ReadVarBytes(252)), this))
                            State |= VMState.FAULT;
                        break;

                    // Stack ops
                    case OpCode.DUPFROMALTSTACK:
                        EvaluationStack.Push(AltStack.Peek());
                        break;
                    case OpCode.TOALTSTACK:
                        AltStack.Push(EvaluationStack.Pop());
                        break;
                    case OpCode.FROMALTSTACK:
                        EvaluationStack.Push(AltStack.Pop());
                        break;
                    case OpCode.XDROP:
                        {
                            int n = (int)EvaluationStack.Pop().GetBigInteger();
                            if (n < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            EvaluationStack.Remove(n);
                        }
                        break;
                    case OpCode.XSWAP:
                        {
                            int n = (int)EvaluationStack.Pop().GetBigInteger();
                            if (n < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            if (n == 0) break;
                            StackItem xn = EvaluationStack.Peek(n);
                            EvaluationStack.Set(n, EvaluationStack.Peek());
                            EvaluationStack.Set(0, xn);
                        }
                        break;
                    case OpCode.XTUCK:
                        {
                            int n = (int)EvaluationStack.Pop().GetBigInteger();
                            if (n <= 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            EvaluationStack.Insert(n, EvaluationStack.Peek());
                        }
                        break;
                    case OpCode.DEPTH:
                        EvaluationStack.Push(EvaluationStack.Count);
                        break;
                    case OpCode.DROP:
                        EvaluationStack.Pop();
                        break;
                    case OpCode.DUP:
                        EvaluationStack.Push(EvaluationStack.Peek());
                        break;
                    case OpCode.NIP:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            EvaluationStack.Pop();
                            EvaluationStack.Push(x2);
                        }
                        break;
                    case OpCode.OVER:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Peek();
                            EvaluationStack.Push(x2);
                            EvaluationStack.Push(x1);
                        }
                        break;
                    case OpCode.PICK:
                        {
                            int n = (int)EvaluationStack.Pop().GetBigInteger();
                            if (n < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            EvaluationStack.Push(EvaluationStack.Peek(n));
                        }
                        break;
                    case OpCode.ROLL:
                        {
                            int n = (int)EvaluationStack.Pop().GetBigInteger();
                            if (n < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            if (n == 0) break;
                            EvaluationStack.Push(EvaluationStack.Remove(n));
                        }
                        break;
                    case OpCode.ROT:
                        {
                            StackItem x3 = EvaluationStack.Pop();
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x2);
                            EvaluationStack.Push(x3);
                            EvaluationStack.Push(x1);
                        }
                        break;
                    case OpCode.SWAP:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x2);
                            EvaluationStack.Push(x1);
                        }
                        break;
                    case OpCode.TUCK:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x2);
                            EvaluationStack.Push(x1);
                            EvaluationStack.Push(x2);
                        }
                        break;
                    case OpCode.CAT:
                        {
                            byte[] x2 = EvaluationStack.Pop().GetByteArray();
                            byte[] x1 = EvaluationStack.Pop().GetByteArray();
                            EvaluationStack.Push(x1.Concat(x2).ToArray());
                        }
                        break;
                    case OpCode.SUBSTR:
                        {
                            int count = (int)EvaluationStack.Pop().GetBigInteger();
                            if (count < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            int index = (int)EvaluationStack.Pop().GetBigInteger();
                            if (index < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[] x = EvaluationStack.Pop().GetByteArray();
                            EvaluationStack.Push(x.Skip(index).Take(count).ToArray());
                        }
                        break;
                    case OpCode.LEFT:
                        {
                            int count = (int)EvaluationStack.Pop().GetBigInteger();
                            if (count < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[] x = EvaluationStack.Pop().GetByteArray();
                            EvaluationStack.Push(x.Take(count).ToArray());
                        }
                        break;
                    case OpCode.RIGHT:
                        {
                            int count = (int)EvaluationStack.Pop().GetBigInteger();
                            if (count < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[] x = EvaluationStack.Pop().GetByteArray();
                            if (x.Length < count)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            EvaluationStack.Push(x.Skip(x.Length - count).ToArray());
                        }
                        break;
                    case OpCode.SIZE:
                        {
                            byte[] x = EvaluationStack.Pop().GetByteArray();
                            EvaluationStack.Push(x.Length);
                        }
                        break;

                    // Bitwise logic
                    case OpCode.INVERT:
                        {
                            BigInteger x = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(~x);
                        }
                        break;
                    case OpCode.AND:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 & x2);
                        }
                        break;
                    case OpCode.OR:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 | x2);
                        }
                        break;
                    case OpCode.XOR:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 ^ x2);
                        }
                        break;
                    case OpCode.EQUAL:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x1.Equals(x2));
                        }
                        break;

                    // Numeric
                    case OpCode.INC:
                        {
                            BigInteger x = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x + 1);
                        }
                        break;
                    case OpCode.DEC:
                        {
                            BigInteger x = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x - 1);
                        }
                        break;
                    case OpCode.SIGN:
                        {
                            BigInteger x = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x.Sign);
                        }
                        break;
                    case OpCode.NEGATE:
                        {
                            BigInteger x = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(-x);
                        }
                        break;
                    case OpCode.ABS:
                        {
                            BigInteger x = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(BigInteger.Abs(x));
                        }
                        break;
                    case OpCode.NOT:
                        {
                            bool x = EvaluationStack.Pop().GetBoolean();
                            EvaluationStack.Push(!x);
                        }
                        break;
                    case OpCode.NZ:
                        {
                            BigInteger x = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x != BigInteger.Zero);
                        }
                        break;
                    case OpCode.ADD:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 + x2);
                        }
                        break;
                    case OpCode.SUB:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 - x2);
                        }
                        break;
                    case OpCode.MUL:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 * x2);
                        }
                        break;
                    case OpCode.DIV:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 / x2);
                        }
                        break;
                    case OpCode.MOD:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 % x2);
                        }
                        break;
                    case OpCode.SHL:
                        {
                            int n = (int)EvaluationStack.Pop().GetBigInteger();
                            BigInteger x = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x << n);
                        }
                        break;
                    case OpCode.SHR:
                        {
                            int n = (int)EvaluationStack.Pop().GetBigInteger();
                            BigInteger x = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x >> n);
                        }
                        break;
                    case OpCode.BOOLAND:
                        {
                            bool x2 = EvaluationStack.Pop().GetBoolean();
                            bool x1 = EvaluationStack.Pop().GetBoolean();
                            EvaluationStack.Push(x1 && x2);
                        }
                        break;
                    case OpCode.BOOLOR:
                        {
                            bool x2 = EvaluationStack.Pop().GetBoolean();
                            bool x1 = EvaluationStack.Pop().GetBoolean();
                            EvaluationStack.Push(x1 || x2);
                        }
                        break;
                    case OpCode.NUMEQUAL:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 == x2);
                        }
                        break;
                    case OpCode.NUMNOTEQUAL:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 != x2);
                        }
                        break;
                    case OpCode.LT:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 < x2);
                        }
                        break;
                    case OpCode.GT:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 > x2);
                        }
                        break;
                    case OpCode.LTE:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 <= x2);
                        }
                        break;
                    case OpCode.GTE:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(x1 >= x2);
                        }
                        break;
                    case OpCode.MIN:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(BigInteger.Min(x1, x2));
                        }
                        break;
                    case OpCode.MAX:
                        {
                            BigInteger x2 = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(BigInteger.Max(x1, x2));
                        }
                        break;
                    case OpCode.WITHIN:
                        {
                            BigInteger b = EvaluationStack.Pop().GetBigInteger();
                            BigInteger a = EvaluationStack.Pop().GetBigInteger();
                            BigInteger x = EvaluationStack.Pop().GetBigInteger();
                            EvaluationStack.Push(a <= x && x < b);
                        }
                        break;

                    // Crypto
                    case OpCode.SHA1:
                        using (SHA1 sha = SHA1.Create())
                        {
                            byte[] x = EvaluationStack.Pop().GetByteArray();
                            EvaluationStack.Push(sha.ComputeHash(x));
                        }
                        break;
                    case OpCode.SHA256:
                        using (SHA256 sha = SHA256.Create())
                        {
                            byte[] x = EvaluationStack.Pop().GetByteArray();
                            EvaluationStack.Push(sha.ComputeHash(x));
                        }
                        break;
                    case OpCode.HASH160:
                        {
                            byte[] x = EvaluationStack.Pop().GetByteArray();
                            EvaluationStack.Push(Crypto.Hash160(x));
                        }
                        break;
                    case OpCode.HASH256:
                        {
                            byte[] x = EvaluationStack.Pop().GetByteArray();
                            EvaluationStack.Push(Crypto.Hash256(x));
                        }
                        break;
                    case OpCode.CHECKSIG:
                        {
                            byte[] pubkey = EvaluationStack.Pop().GetByteArray();
                            byte[] signature = EvaluationStack.Pop().GetByteArray();
                            try
                            {
                                EvaluationStack.Push(Crypto.VerifySignature(ScriptContainer.GetMessage(), signature, pubkey));
                            }
                            catch (ArgumentException)
                            {
                                EvaluationStack.Push(false);
                            }
                        }
                        break;
                    case OpCode.VERIFY:
                        {
                            byte[] pubkey = EvaluationStack.Pop().GetByteArray();
                            byte[] signature = EvaluationStack.Pop().GetByteArray();
                            byte[] message = EvaluationStack.Pop().GetByteArray();
                            try
                            {
                                EvaluationStack.Push(Crypto.VerifySignature(message, signature, pubkey));
                            }
                            catch (ArgumentException)
                            {
                                EvaluationStack.Push(false);
                            }
                        }
                        break;
                    case OpCode.CHECKMULTISIG:
                        {
                            int n;
                            byte[][] pubkeys;
                            StackItem item = EvaluationStack.Pop();
                            if (item is VMArray array1)
                            {
                                pubkeys = array1.Select(p => p.GetByteArray()).ToArray();
                                n = pubkeys.Length;
                                if (n == 0)
                                {
                                    State |= VMState.FAULT;
                                    return;
                                }
                            }
                            else
                            {
                                n = (int)item.GetBigInteger();
                                if (n < 1 || n > EvaluationStack.Count)
                                {
                                    State |= VMState.FAULT;
                                    return;
                                }
                                pubkeys = new byte[n][];
                                for (int i = 0; i < n; i++)
                                    pubkeys[i] = EvaluationStack.Pop().GetByteArray();
                            }
                            int m;
                            byte[][] signatures;
                            item = EvaluationStack.Pop();
                            if (item is VMArray array2)
                            {
                                signatures = array2.Select(p => p.GetByteArray()).ToArray();
                                m = signatures.Length;
                                if (m == 0 || m > n)
                                {
                                    State |= VMState.FAULT;
                                    return;
                                }
                            }
                            else
                            {
                                m = (int)item.GetBigInteger();
                                if (m < 1 || m > n || m > EvaluationStack.Count)
                                {
                                    State |= VMState.FAULT;
                                    return;
                                }
                                signatures = new byte[m][];
                                for (int i = 0; i < m; i++)
                                    signatures[i] = EvaluationStack.Pop().GetByteArray();
                            }
                            byte[] message = ScriptContainer.GetMessage();
                            bool fSuccess = true;
                            try
                            {
                                for (int i = 0, j = 0; fSuccess && i < m && j < n;)
                                {
                                    if (Crypto.VerifySignature(message, signatures[i], pubkeys[j]))
                                        i++;
                                    j++;
                                    if (m - i > n - j)
                                        fSuccess = false;
                                }
                            }
                            catch (ArgumentException)
                            {
                                fSuccess = false;
                            }
                            EvaluationStack.Push(fSuccess);
                        }
                        break;

                    // Array
                    case OpCode.ARRAYSIZE:
                        {
                            StackItem item = EvaluationStack.Pop();
                            if (item is ICollection collection)
                                EvaluationStack.Push(collection.Count);
                            else
                                EvaluationStack.Push(item.GetByteArray().Length);
                        }
                        break;
                    case OpCode.PACK:
                        {
                            int size = (int)EvaluationStack.Pop().GetBigInteger();
                            if (size < 0 || size > EvaluationStack.Count)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            List<StackItem> items = new List<StackItem>(size);
                            for (int i = 0; i < size; i++)
                                items.Add(EvaluationStack.Pop());
                            EvaluationStack.Push(items);
                        }
                        break;
                    case OpCode.UNPACK:
                        {
                            StackItem item = EvaluationStack.Pop();
                            if (item is VMArray array)
                            {
                                for (int i = array.Count - 1; i >= 0; i--)
                                    EvaluationStack.Push(array[i]);
                                EvaluationStack.Push(array.Count);
                            }
                            else
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                        }
                        break;
                    case OpCode.PICKITEM:
                        {
                            StackItem key = EvaluationStack.Pop();
                            if (key is ICollection)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            switch (EvaluationStack.Pop())
                            {
                                case VMArray array:
                                    int index = (int)key.GetBigInteger();
                                    if (index < 0 || index >= array.Count)
                                    {
                                        State |= VMState.FAULT;
                                        return;
                                    }
                                    EvaluationStack.Push(array[index]);
                                    break;
                                case Map map:
                                    if (map.TryGetValue(key, out StackItem value))
                                    {
                                        EvaluationStack.Push(value);
                                    }
                                    else
                                    {
                                        State |= VMState.FAULT;
                                        return;
                                    }
                                    break;
                                default:
                                    State |= VMState.FAULT;
                                    return;
                            }
                        }
                        break;
                    case OpCode.SETITEM:
                        {
                            StackItem value = EvaluationStack.Pop();
                            if (value is Struct s) value = s.Clone();
                            StackItem key = EvaluationStack.Pop();
                            if (key is ICollection)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            switch (EvaluationStack.Pop())
                            {
                                case VMArray array:
                                    int index = (int)key.GetBigInteger();
                                    if (index < 0 || index >= array.Count)
                                    {
                                        State |= VMState.FAULT;
                                        return;
                                    }
                                    array[index] = value;
                                    break;
                                case Map map:
                                    map[key] = value;
                                    break;
                                default:
                                    State |= VMState.FAULT;
                                    return;
                            }
                        }
                        break;
                    case OpCode.NEWARRAY:
                        {
                            int count = (int)EvaluationStack.Pop().GetBigInteger();
                            List<StackItem> items = new List<StackItem>(count);
                            for (var i = 0; i < count; i++)
                            {
                                items.Add(false);
                            }
                            EvaluationStack.Push(new Types.Array(items));
                        }
                        break;
                    case OpCode.NEWSTRUCT:
                        {
                            int count = (int)EvaluationStack.Pop().GetBigInteger();
                            List<StackItem> items = new List<StackItem>(count);
                            for (var i = 0; i < count; i++)
                            {
                                items.Add(false);
                            }
                            EvaluationStack.Push(new VM.Types.Struct(items));
                        }
                        break;
                    case OpCode.NEWMAP:
                        EvaluationStack.Push(new Map());
                        break;
                    case OpCode.APPEND:
                        {
                            StackItem newItem = EvaluationStack.Pop();
                            if (newItem is Types.Struct s)
                            {
                                newItem = s.Clone();
                            }
                            StackItem arrItem = EvaluationStack.Pop();
                            if (arrItem is VMArray array)
                            {
                                array.Add(newItem);
                            }
                            else
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                        }
                        break;
                    case OpCode.REVERSE:
                        {
                            StackItem arrItem = EvaluationStack.Pop();
                            if (arrItem is VMArray array)
                            {
                                array.Reverse();
                            }
                            else
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                        }
                        break;
                    case OpCode.REMOVE:
                        {
                            StackItem key = EvaluationStack.Pop();
                            if (key is ICollection)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            switch (EvaluationStack.Pop())
                            {
                                case VMArray array:
                                    int index = (int)key.GetBigInteger();
                                    if (index < 0 || index >= array.Count)
                                    {
                                        State |= VMState.FAULT;
                                        return;
                                    }
                                    array.RemoveAt(index);
                                    break;
                                case Map map:
                                    map.Remove(key);
                                    break;
                                default:
                                    State |= VMState.FAULT;
                                    return;
                            }
                        }
                        break;
                    case OpCode.HASKEY:
                        {
                            StackItem key = EvaluationStack.Pop();
                            if (key is ICollection)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            switch (EvaluationStack.Pop())
                            {
                                case VMArray array:
                                    int index = (int)key.GetBigInteger();
                                    if (index < 0)
                                    {
                                        State |= VMState.FAULT;
                                        return;
                                    }
                                    EvaluationStack.Push(index < array.Count);
                                    break;
                                case Map map:
                                    EvaluationStack.Push(map.ContainsKey(key));
                                    break;
                                default:
                                    State |= VMState.FAULT;
                                    return;
                            }
                        }
                        break;
                    case OpCode.KEYS:
                        switch (EvaluationStack.Pop())
                        {
                            case Map map:
                                EvaluationStack.Push(new VMArray(map.Keys));
                                break;
                            default:
                                State |= VMState.FAULT;
                                return;
                        }
                        break;
                    case OpCode.VALUES:
                        {
                            ICollection<StackItem> values;
                            switch (EvaluationStack.Pop())
                            {
                                case VMArray array:
                                    values = array;
                                    break;
                                case Map map:
                                    values = map.Values;
                                    break;
                                default:
                                    State |= VMState.FAULT;
                                    return;
                            }
                            List<StackItem> newArray = new List<StackItem>(values.Count);
                            foreach (StackItem item in values)
                                if (item is Struct s)
                                    newArray.Add(s.Clone());
                                else
                                    newArray.Add(item);
                            EvaluationStack.Push(new VMArray(newArray));
                        }
                        break;

                    // Exceptions
                    case OpCode.THROW:
                        State |= VMState.FAULT;
                        return;
                    case OpCode.THROWIFNOT:
                        if (!EvaluationStack.Pop().GetBoolean())
                        {
                            State |= VMState.FAULT;
                            return;
                        }
                        break;

                    default:
                        State |= VMState.FAULT;
                        return;
                }
            if (!State.HasFlag(VMState.FAULT) && InvocationStack.Count > 0)
            {
                if (CurrentContext.BreakPoints.Contains((uint)CurrentContext.InstructionPointer))
                    State |= VMState.BREAK;
            }
        }

        public void LoadScript(byte[] script, bool push_only = false)
        {
            InvocationStack.Push(new ExecutionContext(this, script, push_only));
        }

        public bool RemoveBreakPoint(uint position)
        {
            if (InvocationStack.Count == 0) return false;
            return CurrentContext.BreakPoints.Remove(position);
        }

        public void StepInto()
        {
            if (InvocationStack.Count == 0) State |= VMState.HALT;
            if (State.HasFlag(VMState.HALT) || State.HasFlag(VMState.FAULT)) return;
            OpCode opcode = CurrentContext.InstructionPointer >= CurrentContext.Script.Length ? OpCode.RET : (OpCode)CurrentContext.OpReader.ReadByte();
            try
            {
                ExecuteOp(opcode, CurrentContext);
            }
            catch
            {
                State |= VMState.FAULT;
            }
        }

        public void StepOut()
        {
            State &= ~VMState.BREAK;
            int c = InvocationStack.Count;
            while (!State.HasFlag(VMState.HALT) && !State.HasFlag(VMState.FAULT) && !State.HasFlag(VMState.BREAK) && InvocationStack.Count >= c)
                StepInto();
        }

        public void StepOver()
        {
            if (State.HasFlag(VMState.HALT) || State.HasFlag(VMState.FAULT)) return;
            State &= ~VMState.BREAK;
            int c = InvocationStack.Count;
            do
            {
                StepInto();
            } while (!State.HasFlag(VMState.HALT) && !State.HasFlag(VMState.FAULT) && !State.HasFlag(VMState.BREAK) && InvocationStack.Count > c);
        }
    }
}
