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
        private readonly Dictionary<byte[], HashSet<uint>> break_points = new Dictionary<byte[], HashSet<uint>>(new HashComparer());

        public IScriptContainer ScriptContainer { get; }
        public ICrypto Crypto { get; }
        public InteropService Service { get; }
        public RandomAccessStack<ExecutionContext> InvocationStack { get; } = new RandomAccessStack<ExecutionContext>();
        public RandomAccessStack<StackItem> ResultStack { get; } = new RandomAccessStack<StackItem>();
        public ExecutionContext CurrentContext => InvocationStack.Peek();
        public ExecutionContext CallingContext => InvocationStack.Count > 1 ? InvocationStack.Peek(1) : null;
        public ExecutionContext EntryContext => InvocationStack.Peek(InvocationStack.Count - 1);
        public VMState State { get; protected set; } = VMState.BREAK;

        public ExecutionEngine(IScriptContainer container, ICrypto crypto, IScriptTable table = null, InteropService service = null)
        {
            this.ScriptContainer = container;
            this.Crypto = crypto;
            this.table = table;
            this.Service = service ?? new InteropService();
        }

        public void AddBreakPoint(byte[] script_hash, uint position)
        {
            if (!break_points.TryGetValue(script_hash, out HashSet<uint> hashset))
            {
                hashset = new HashSet<uint>();
                break_points.Add(script_hash, hashset);
            }
            hashset.Add(position);
        }

        public virtual void Dispose()
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
            if (opcode >= OpCode.PUSHBYTES1 && opcode <= OpCode.PUSHBYTES75)
                context.EvaluationStack.Push(context.OpReader.ReadBytes((byte)opcode));
            else
                switch (opcode)
                {
                    // Push value
                    case OpCode.PUSH0:
                        context.EvaluationStack.Push(new byte[0]);
                        break;
                    case OpCode.PUSHDATA1:
                        context.EvaluationStack.Push(context.OpReader.ReadBytes(context.OpReader.ReadByte()));
                        break;
                    case OpCode.PUSHDATA2:
                        context.EvaluationStack.Push(context.OpReader.ReadBytes(context.OpReader.ReadUInt16()));
                        break;
                    case OpCode.PUSHDATA4:
                        context.EvaluationStack.Push(context.OpReader.ReadBytes(context.OpReader.ReadInt32()));
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
                        context.EvaluationStack.Push((int)opcode - (int)OpCode.PUSH1 + 1);
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
                                fValue = context.EvaluationStack.Pop().GetBoolean();
                                if (opcode == OpCode.JMPIFNOT)
                                    fValue = !fValue;
                            }
                            if (fValue)
                                context.InstructionPointer = offset;
                        }
                        break;
                    case OpCode.CALL:
                        {
                            ExecutionContext context_call = LoadScript(context.Script);
                            context.EvaluationStack.CopyTo(context_call.EvaluationStack);
                            context_call.InstructionPointer = context.InstructionPointer;
                            context.EvaluationStack.Clear();
                            context.InstructionPointer += 2;
                            ExecuteOp(OpCode.JMP, context_call);
                        }
                        break;
                    case OpCode.RET:
                        using (ExecutionContext context_pop = InvocationStack.Pop())
                        {
                            int rvcount = context_pop.RVCount;
                            if (rvcount == -1) rvcount = context_pop.EvaluationStack.Count;
                            if (rvcount > 0)
                            {
                                if (context_pop.EvaluationStack.Count < rvcount)
                                {
                                    State |= VMState.FAULT;
                                    return;
                                }
                                RandomAccessStack<StackItem> stack_eval;
                                if (InvocationStack.Count == 0)
                                    stack_eval = ResultStack;
                                else
                                    stack_eval = CurrentContext.EvaluationStack;
                                context_pop.EvaluationStack.CopyTo(stack_eval, rvcount);
                            }
                            if (context_pop.RVCount == -1 && InvocationStack.Count > 0)
                            {
                                context_pop.AltStack.CopyTo(CurrentContext.AltStack);
                            }
                        }
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
                                script_hash = context.EvaluationStack.Pop().GetByteArray();
                            }

                            byte[] script = table.GetScript(script_hash);
                            if (script == null)
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            ExecutionContext context_new = LoadScript(script);
                            context.EvaluationStack.CopyTo(context_new.EvaluationStack);

                            if (opcode == OpCode.TAILCALL)
                                InvocationStack.Remove(1).Dispose();
                            else
                                context.EvaluationStack.Clear();
                        }
                        break;
                    case OpCode.SYSCALL:
                        if (!Service.Invoke(Encoding.ASCII.GetString(context.OpReader.ReadVarBytes(252)), this))
                            State |= VMState.FAULT;
                        break;

                    // Stack ops
                    case OpCode.DUPFROMALTSTACK:
                        context.EvaluationStack.Push(context.AltStack.Peek());
                        break;
                    case OpCode.TOALTSTACK:
                        context.AltStack.Push(context.EvaluationStack.Pop());
                        break;
                    case OpCode.FROMALTSTACK:
                        context.EvaluationStack.Push(context.AltStack.Pop());
                        break;
                    case OpCode.XDROP:
                        {
                            int n = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (n < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            context.EvaluationStack.Remove(n);
                        }
                        break;
                    case OpCode.XSWAP:
                        {
                            int n = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (n < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            if (n == 0) break;
                            StackItem xn = context.EvaluationStack.Peek(n);
                            context.EvaluationStack.Set(n, context.EvaluationStack.Peek());
                            context.EvaluationStack.Set(0, xn);
                        }
                        break;
                    case OpCode.XTUCK:
                        {
                            int n = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (n <= 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            context.EvaluationStack.Insert(n, context.EvaluationStack.Peek());
                        }
                        break;
                    case OpCode.DEPTH:
                        context.EvaluationStack.Push(context.EvaluationStack.Count);
                        break;
                    case OpCode.DROP:
                        context.EvaluationStack.Pop();
                        break;
                    case OpCode.DUP:
                        context.EvaluationStack.Push(context.EvaluationStack.Peek());
                        break;
                    case OpCode.NIP:
                        context.EvaluationStack.Remove(1);
                        break;
                    case OpCode.OVER:
                        context.EvaluationStack.Push(context.EvaluationStack.Peek(1));
                        break;
                    case OpCode.PICK:
                        {
                            int n = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (n < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            context.EvaluationStack.Push(context.EvaluationStack.Peek(n));
                        }
                        break;
                    case OpCode.ROLL:
                        {
                            int n = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (n < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            if (n == 0) break;
                            context.EvaluationStack.Push(context.EvaluationStack.Remove(n));
                        }
                        break;
                    case OpCode.ROT:
                        context.EvaluationStack.Push(context.EvaluationStack.Remove(2));
                        break;
                    case OpCode.SWAP:
                        context.EvaluationStack.Push(context.EvaluationStack.Remove(1));
                        break;
                    case OpCode.TUCK:
                        context.EvaluationStack.Insert(2, context.EvaluationStack.Peek());
                        break;
                    case OpCode.CAT:
                        {
                            byte[] x2 = context.EvaluationStack.Pop().GetByteArray();
                            byte[] x1 = context.EvaluationStack.Pop().GetByteArray();
                            context.EvaluationStack.Push(x1.Concat(x2).ToArray());
                        }
                        break;
                    case OpCode.SUBSTR:
                        {
                            int count = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (count < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            int index = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (index < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[] x = context.EvaluationStack.Pop().GetByteArray();
                            context.EvaluationStack.Push(x.Skip(index).Take(count).ToArray());
                        }
                        break;
                    case OpCode.LEFT:
                        {
                            int count = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (count < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[] x = context.EvaluationStack.Pop().GetByteArray();
                            context.EvaluationStack.Push(x.Take(count).ToArray());
                        }
                        break;
                    case OpCode.RIGHT:
                        {
                            int count = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (count < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[] x = context.EvaluationStack.Pop().GetByteArray();
                            if (x.Length < count)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            context.EvaluationStack.Push(x.Skip(x.Length - count).ToArray());
                        }
                        break;
                    case OpCode.SIZE:
                        {
                            byte[] x = context.EvaluationStack.Pop().GetByteArray();
                            context.EvaluationStack.Push(x.Length);
                        }
                        break;

                    // Bitwise logic
                    case OpCode.INVERT:
                        {
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(~x);
                        }
                        break;
                    case OpCode.AND:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 & x2);
                        }
                        break;
                    case OpCode.OR:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 | x2);
                        }
                        break;
                    case OpCode.XOR:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 ^ x2);
                        }
                        break;
                    case OpCode.EQUAL:
                        {
                            StackItem x2 = context.EvaluationStack.Pop();
                            StackItem x1 = context.EvaluationStack.Pop();
                            context.EvaluationStack.Push(x1.Equals(x2));
                        }
                        break;

                    // Numeric
                    case OpCode.INC:
                        {
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x + 1);
                        }
                        break;
                    case OpCode.DEC:
                        {
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x - 1);
                        }
                        break;
                    case OpCode.SIGN:
                        {
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x.Sign);
                        }
                        break;
                    case OpCode.NEGATE:
                        {
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(-x);
                        }
                        break;
                    case OpCode.ABS:
                        {
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(BigInteger.Abs(x));
                        }
                        break;
                    case OpCode.NOT:
                        {
                            bool x = context.EvaluationStack.Pop().GetBoolean();
                            context.EvaluationStack.Push(!x);
                        }
                        break;
                    case OpCode.NZ:
                        {
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x != BigInteger.Zero);
                        }
                        break;
                    case OpCode.ADD:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 + x2);
                        }
                        break;
                    case OpCode.SUB:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 - x2);
                        }
                        break;
                    case OpCode.MUL:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 * x2);
                        }
                        break;
                    case OpCode.DIV:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 / x2);
                        }
                        break;
                    case OpCode.MOD:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 % x2);
                        }
                        break;
                    case OpCode.SHL:
                        {
                            int n = (int)context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x << n);
                        }
                        break;
                    case OpCode.SHR:
                        {
                            int n = (int)context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x >> n);
                        }
                        break;
                    case OpCode.BOOLAND:
                        {
                            bool x2 = context.EvaluationStack.Pop().GetBoolean();
                            bool x1 = context.EvaluationStack.Pop().GetBoolean();
                            context.EvaluationStack.Push(x1 && x2);
                        }
                        break;
                    case OpCode.BOOLOR:
                        {
                            bool x2 = context.EvaluationStack.Pop().GetBoolean();
                            bool x1 = context.EvaluationStack.Pop().GetBoolean();
                            context.EvaluationStack.Push(x1 || x2);
                        }
                        break;
                    case OpCode.NUMEQUAL:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 == x2);
                        }
                        break;
                    case OpCode.NUMNOTEQUAL:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 != x2);
                        }
                        break;
                    case OpCode.LT:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 < x2);
                        }
                        break;
                    case OpCode.GT:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 > x2);
                        }
                        break;
                    case OpCode.LTE:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 <= x2);
                        }
                        break;
                    case OpCode.GTE:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 >= x2);
                        }
                        break;
                    case OpCode.MIN:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(BigInteger.Min(x1, x2));
                        }
                        break;
                    case OpCode.MAX:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(BigInteger.Max(x1, x2));
                        }
                        break;
                    case OpCode.WITHIN:
                        {
                            BigInteger b = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger a = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(a <= x && x < b);
                        }
                        break;

                    // Crypto
                    case OpCode.SHA1:
                        using (SHA1 sha = SHA1.Create())
                        {
                            byte[] x = context.EvaluationStack.Pop().GetByteArray();
                            context.EvaluationStack.Push(sha.ComputeHash(x));
                        }
                        break;
                    case OpCode.SHA256:
                        using (SHA256 sha = SHA256.Create())
                        {
                            byte[] x = context.EvaluationStack.Pop().GetByteArray();
                            context.EvaluationStack.Push(sha.ComputeHash(x));
                        }
                        break;
                    case OpCode.HASH160:
                        {
                            byte[] x = context.EvaluationStack.Pop().GetByteArray();
                            context.EvaluationStack.Push(Crypto.Hash160(x));
                        }
                        break;
                    case OpCode.HASH256:
                        {
                            byte[] x = context.EvaluationStack.Pop().GetByteArray();
                            context.EvaluationStack.Push(Crypto.Hash256(x));
                        }
                        break;
                    case OpCode.CHECKSIG:
                        {
                            byte[] pubkey = context.EvaluationStack.Pop().GetByteArray();
                            byte[] signature = context.EvaluationStack.Pop().GetByteArray();
                            try
                            {
                                context.EvaluationStack.Push(Crypto.VerifySignature(ScriptContainer.GetMessage(), signature, pubkey));
                            }
                            catch (ArgumentException)
                            {
                                context.EvaluationStack.Push(false);
                            }
                        }
                        break;
                    case OpCode.VERIFY:
                        {
                            byte[] pubkey = context.EvaluationStack.Pop().GetByteArray();
                            byte[] signature = context.EvaluationStack.Pop().GetByteArray();
                            byte[] message = context.EvaluationStack.Pop().GetByteArray();
                            try
                            {
                                context.EvaluationStack.Push(Crypto.VerifySignature(message, signature, pubkey));
                            }
                            catch (ArgumentException)
                            {
                                context.EvaluationStack.Push(false);
                            }
                        }
                        break;
                    case OpCode.CHECKMULTISIG:
                        {
                            int n;
                            byte[][] pubkeys;
                            StackItem item = context.EvaluationStack.Pop();
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
                                if (n < 1 || n > context.EvaluationStack.Count)
                                {
                                    State |= VMState.FAULT;
                                    return;
                                }
                                pubkeys = new byte[n][];
                                for (int i = 0; i < n; i++)
                                    pubkeys[i] = context.EvaluationStack.Pop().GetByteArray();
                            }
                            int m;
                            byte[][] signatures;
                            item = context.EvaluationStack.Pop();
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
                                if (m < 1 || m > n || m > context.EvaluationStack.Count)
                                {
                                    State |= VMState.FAULT;
                                    return;
                                }
                                signatures = new byte[m][];
                                for (int i = 0; i < m; i++)
                                    signatures[i] = context.EvaluationStack.Pop().GetByteArray();
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
                            context.EvaluationStack.Push(fSuccess);
                        }
                        break;

                    // Array
                    case OpCode.ARRAYSIZE:
                        {
                            StackItem item = context.EvaluationStack.Pop();
                            if (item is ICollection collection)
                                context.EvaluationStack.Push(collection.Count);
                            else
                                context.EvaluationStack.Push(item.GetByteArray().Length);
                        }
                        break;
                    case OpCode.PACK:
                        {
                            int size = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (size < 0 || size > context.EvaluationStack.Count)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            List<StackItem> items = new List<StackItem>(size);
                            for (int i = 0; i < size; i++)
                                items.Add(context.EvaluationStack.Pop());
                            context.EvaluationStack.Push(items);
                        }
                        break;
                    case OpCode.UNPACK:
                        {
                            StackItem item = context.EvaluationStack.Pop();
                            if (item is VMArray array)
                            {
                                for (int i = array.Count - 1; i >= 0; i--)
                                    context.EvaluationStack.Push(array[i]);
                                context.EvaluationStack.Push(array.Count);
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
                            StackItem key = context.EvaluationStack.Pop();
                            if (key is ICollection)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            switch (context.EvaluationStack.Pop())
                            {
                                case VMArray array:
                                    int index = (int)key.GetBigInteger();
                                    if (index < 0 || index >= array.Count)
                                    {
                                        State |= VMState.FAULT;
                                        return;
                                    }
                                    context.EvaluationStack.Push(array[index]);
                                    break;
                                case Map map:
                                    if (map.TryGetValue(key, out StackItem value))
                                    {
                                        context.EvaluationStack.Push(value);
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
                            StackItem value = context.EvaluationStack.Pop();
                            if (value is Struct s) value = s.Clone();
                            StackItem key = context.EvaluationStack.Pop();
                            if (key is ICollection)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            switch (context.EvaluationStack.Pop())
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
                            int count = (int)context.EvaluationStack.Pop().GetBigInteger();
                            List<StackItem> items = new List<StackItem>(count);
                            for (var i = 0; i < count; i++)
                            {
                                items.Add(false);
                            }
                            context.EvaluationStack.Push(new Types.Array(items));
                        }
                        break;
                    case OpCode.NEWSTRUCT:
                        {
                            int count = (int)context.EvaluationStack.Pop().GetBigInteger();
                            List<StackItem> items = new List<StackItem>(count);
                            for (var i = 0; i < count; i++)
                            {
                                items.Add(false);
                            }
                            context.EvaluationStack.Push(new VM.Types.Struct(items));
                        }
                        break;
                    case OpCode.NEWMAP:
                        context.EvaluationStack.Push(new Map());
                        break;
                    case OpCode.APPEND:
                        {
                            StackItem newItem = context.EvaluationStack.Pop();
                            if (newItem is Types.Struct s)
                            {
                                newItem = s.Clone();
                            }
                            StackItem arrItem = context.EvaluationStack.Pop();
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
                            StackItem arrItem = context.EvaluationStack.Pop();
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
                            StackItem key = context.EvaluationStack.Pop();
                            if (key is ICollection)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            switch (context.EvaluationStack.Pop())
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
                            StackItem key = context.EvaluationStack.Pop();
                            if (key is ICollection)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            switch (context.EvaluationStack.Pop())
                            {
                                case VMArray array:
                                    int index = (int)key.GetBigInteger();
                                    if (index < 0)
                                    {
                                        State |= VMState.FAULT;
                                        return;
                                    }
                                    context.EvaluationStack.Push(index < array.Count);
                                    break;
                                case Map map:
                                    context.EvaluationStack.Push(map.ContainsKey(key));
                                    break;
                                default:
                                    State |= VMState.FAULT;
                                    return;
                            }
                        }
                        break;
                    case OpCode.KEYS:
                        switch (context.EvaluationStack.Pop())
                        {
                            case Map map:
                                context.EvaluationStack.Push(new VMArray(map.Keys));
                                break;
                            default:
                                State |= VMState.FAULT;
                                return;
                        }
                        break;
                    case OpCode.VALUES:
                        {
                            ICollection<StackItem> values;
                            switch (context.EvaluationStack.Pop())
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
                            context.EvaluationStack.Push(new VMArray(newArray));
                        }
                        break;

                    // Stack isolation
                    case OpCode.CALL_I:
                        {
                            int rvcount = context.OpReader.ReadByte();
                            int pcount = context.OpReader.ReadByte();
                            if (context.EvaluationStack.Count < pcount)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            ExecutionContext context_call = LoadScript(context.Script, rvcount);
                            context.EvaluationStack.CopyTo(context_call.EvaluationStack, pcount);
                            context_call.InstructionPointer = context.InstructionPointer;
                            for (int i = 0; i < pcount; i++)
                                context.EvaluationStack.Pop();
                            context.InstructionPointer += 2;
                            ExecuteOp(OpCode.JMP, context_call);
                        }
                        break;
                    case OpCode.CALL_E:
                    case OpCode.CALL_ED:
                    case OpCode.CALL_ET:
                    case OpCode.CALL_EDT:
                        {
                            if (table == null)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            int rvcount = context.OpReader.ReadByte();
                            int pcount = context.OpReader.ReadByte();
                            if (context.EvaluationStack.Count < pcount)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            if (opcode == OpCode.CALL_ET || opcode == OpCode.CALL_EDT)
                            {
                                if (context.RVCount != rvcount)
                                {
                                    State |= VMState.FAULT;
                                    return;
                                }
                            }
                            byte[] script_hash;
                            if (opcode == OpCode.CALL_ED || opcode == OpCode.CALL_EDT)
                                script_hash = context.EvaluationStack.Pop().GetByteArray();
                            else
                                script_hash = context.OpReader.ReadBytes(20);
                            byte[] script = table.GetScript(script_hash);
                            if (script == null)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            ExecutionContext context_new = LoadScript(script, rvcount);
                            context.EvaluationStack.CopyTo(context_new.EvaluationStack, pcount);
                            if (opcode == OpCode.CALL_ET || opcode == OpCode.CALL_EDT)
                                InvocationStack.Remove(1).Dispose();
                            else
                                for (int i = 0; i < pcount; i++)
                                    context.EvaluationStack.Pop();
                        }
                        break;

                    // Exceptions
                    case OpCode.THROW:
                        State |= VMState.FAULT;
                        return;
                    case OpCode.THROWIFNOT:
                        if (!context.EvaluationStack.Pop().GetBoolean())
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
                if (break_points.TryGetValue(CurrentContext.ScriptHash, out HashSet<uint> hashset) && hashset.Contains((uint)CurrentContext.InstructionPointer))
                    State |= VMState.BREAK;
            }
        }

        public ExecutionContext LoadScript(byte[] script, int rvcount = -1)
        {
            ExecutionContext context = new ExecutionContext(this, script, rvcount);
            InvocationStack.Push(context);
            return context;
        }

        public bool RemoveBreakPoint(byte[] script_hash, uint position)
        {
            if (!break_points.TryGetValue(script_hash, out HashSet<uint> hashset))
                return false;
            if (!hashset.Remove(position))
                return false;
            if (hashset.Count == 0)
                break_points.Remove(script_hash);
            return true;
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
