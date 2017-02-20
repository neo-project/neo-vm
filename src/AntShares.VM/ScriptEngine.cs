using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace AntShares.VM
{
    public class ScriptEngine : IDisposable
    {
        private readonly IScriptTable table;
        private readonly ApiService service;
        private int max_steps;

        private int nOpCount = 0;

        public IScriptContainer ScriptContainer { get; }
        public ICrypto Crypto { get; }
        public RandomAccessStack<ScriptContext> InvocationStack { get; } = new RandomAccessStack<ScriptContext>();
        public RandomAccessStack<StackItem> EvaluationStack { get; } = new RandomAccessStack<StackItem>();
        public RandomAccessStack<StackItem> AltStack { get; } = new RandomAccessStack<StackItem>();
        public byte[] ExecutingScript => InvocationStack.Peek().Script;
        public byte[] CallingScript => InvocationStack.Count > 1 ? InvocationStack.Peek(1).Script : null;
        public byte[] EntryScript => InvocationStack.Peek(InvocationStack.Count - 1).Script;
        public VMState State { get; private set; } = VMState.BREAK;

        public ScriptEngine(IScriptContainer container, ICrypto crypto, int max_steps, IScriptTable table = null, ApiService service = null)
        {
            this.ScriptContainer = container;
            this.Crypto = crypto;
            this.table = table;
            this.service = service ?? new ApiService();
            this.max_steps = max_steps;
        }

        public void AddBreakPoint(uint position)
        {
            InvocationStack.Peek().BreakPoints.Add(position);
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

        private void ExecuteOp(ScriptOp opcode, ScriptContext context)
        {
            if (opcode > ScriptOp.OP_16 && opcode != ScriptOp.OP_RET && context.PushOnly)
            {
                State |= VMState.FAULT;
                return;
            }
            if (opcode > ScriptOp.OP_16 && nOpCount > max_steps)
            {
                State |= VMState.FAULT | VMState.INSUFFICIENT_RESOURCE;
                return;
            }
            if (opcode >= ScriptOp.OP_PUSHBYTES1 && opcode <= ScriptOp.OP_PUSHBYTES75)
                EvaluationStack.Push(context.OpReader.ReadBytes((byte)opcode));
            else
                switch (opcode)
                {
                    // Push value
                    case ScriptOp.OP_0:
                        EvaluationStack.Push(new byte[0]);
                        break;
                    case ScriptOp.OP_PUSHDATA1:
                        EvaluationStack.Push(context.OpReader.ReadBytes(context.OpReader.ReadByte()));
                        break;
                    case ScriptOp.OP_PUSHDATA2:
                        EvaluationStack.Push(context.OpReader.ReadBytes(context.OpReader.ReadUInt16()));
                        break;
                    case ScriptOp.OP_PUSHDATA4:
                        EvaluationStack.Push(context.OpReader.ReadBytes(context.OpReader.ReadInt32()));
                        break;
                    case ScriptOp.OP_1NEGATE:
                    case ScriptOp.OP_1:
                    case ScriptOp.OP_2:
                    case ScriptOp.OP_3:
                    case ScriptOp.OP_4:
                    case ScriptOp.OP_5:
                    case ScriptOp.OP_6:
                    case ScriptOp.OP_7:
                    case ScriptOp.OP_8:
                    case ScriptOp.OP_9:
                    case ScriptOp.OP_10:
                    case ScriptOp.OP_11:
                    case ScriptOp.OP_12:
                    case ScriptOp.OP_13:
                    case ScriptOp.OP_14:
                    case ScriptOp.OP_15:
                    case ScriptOp.OP_16:
                        EvaluationStack.Push(opcode - ScriptOp.OP_1 + 1);
                        break;

                    // Control
                    case ScriptOp.OP_NOP:
                        break;
                    case ScriptOp.OP_JMP:
                    case ScriptOp.OP_JMPIF:
                    case ScriptOp.OP_JMPIFNOT:
                        {
                            int offset = context.OpReader.ReadInt16();
                            offset = context.InstructionPointer + offset - 3;
                            if (offset < 0 || offset > context.Script.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool fValue = true;
                            if (opcode > ScriptOp.OP_JMP)
                            {
                                fValue = EvaluationStack.Pop();
                                if (opcode == ScriptOp.OP_JMPIFNOT)
                                    fValue = !fValue;
                            }
                            if (fValue)
                                context.InstructionPointer = offset;
                        }
                        break;
                    case ScriptOp.OP_CALL:
                        InvocationStack.Push(context.Clone());
                        context.InstructionPointer += 2;
                        ExecuteOp(ScriptOp.OP_JMP, InvocationStack.Peek());
                        break;
                    case ScriptOp.OP_RET:
                        InvocationStack.Pop().Dispose();
                        if (InvocationStack.Count == 0)
                            State |= VMState.HALT;
                        break;
                    case ScriptOp.OP_APPCALL:
                        {
                            if (table == null)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[] script_hash = context.OpReader.ReadBytes(20);
                            byte[] script = table.GetScript(script_hash);
                            if (script == null)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            LoadScript(script);
                        }
                        break;
                    case ScriptOp.OP_SYSCALL:
                        if (!service.Invoke(Encoding.ASCII.GetString(context.OpReader.ReadVarBytes(252)), this))
                            State |= VMState.FAULT;
                        break;

                    // Stack ops
                    case ScriptOp.OP_TOALTSTACK:
                        AltStack.Push(EvaluationStack.Pop());
                        break;
                    case ScriptOp.OP_FROMALTSTACK:
                        EvaluationStack.Push(AltStack.Pop());
                        break;
                    case ScriptOp.OP_XDROP:
                        {
                            int n = (int)(BigInteger)EvaluationStack.Pop();
                            if (n < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            EvaluationStack.Remove(n);
                        }
                        break;
                    //case ScriptOp.OP_2DUP:
                    //    {
                    //        StackItem x2 = EvaluationStack.Pop();
                    //        StackItem x1 = EvaluationStack.Peek();
                    //        EvaluationStack.Push(x2);
                    //        EvaluationStack.Push(x1);
                    //        EvaluationStack.Push(x2);
                    //    }
                    //    break;
                    //case ScriptOp.OP_3DUP:
                    //    {
                    //        StackItem x3 = EvaluationStack.Pop();
                    //        StackItem x2 = EvaluationStack.Pop();
                    //        StackItem x1 = EvaluationStack.Peek();
                    //        EvaluationStack.Push(x2);
                    //        EvaluationStack.Push(x3);
                    //        EvaluationStack.Push(x1);
                    //        EvaluationStack.Push(x2);
                    //        EvaluationStack.Push(x3);
                    //    }
                    //    break;
                    //case ScriptOp.OP_2OVER:
                    //    {
                    //        StackItem x4 = EvaluationStack.Pop();
                    //        StackItem x3 = EvaluationStack.Pop();
                    //        StackItem x2 = EvaluationStack.Pop();
                    //        StackItem x1 = EvaluationStack.Peek();
                    //        EvaluationStack.Push(x2);
                    //        EvaluationStack.Push(x3);
                    //        EvaluationStack.Push(x4);
                    //        EvaluationStack.Push(x1);
                    //        EvaluationStack.Push(x2);
                    //    }
                    //    break;
                    //case ScriptOp.OP_2ROT:
                    //    {
                    //        StackItem x6 = EvaluationStack.Pop();
                    //        StackItem x5 = EvaluationStack.Pop();
                    //        StackItem x4 = EvaluationStack.Pop();
                    //        StackItem x3 = EvaluationStack.Pop();
                    //        StackItem x2 = EvaluationStack.Pop();
                    //        StackItem x1 = EvaluationStack.Pop();
                    //        EvaluationStack.Push(x3);
                    //        EvaluationStack.Push(x4);
                    //        EvaluationStack.Push(x5);
                    //        EvaluationStack.Push(x6);
                    //        EvaluationStack.Push(x1);
                    //        EvaluationStack.Push(x2);
                    //    }
                    //    break;
                    case ScriptOp.OP_XSWAP:
                        {
                            int n = (int)(BigInteger)EvaluationStack.Pop();
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
                    case ScriptOp.OP_XTUCK:
                        {
                            int n = (int)(BigInteger)EvaluationStack.Pop();
                            if (n <= 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            EvaluationStack.Insert(n, EvaluationStack.Peek());
                        }
                        break;
                    case ScriptOp.OP_DEPTH:
                        EvaluationStack.Push(EvaluationStack.Count);
                        break;
                    case ScriptOp.OP_DROP:
                        EvaluationStack.Pop();
                        break;
                    case ScriptOp.OP_DUP:
                        EvaluationStack.Push(EvaluationStack.Peek());
                        break;
                    case ScriptOp.OP_NIP:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            EvaluationStack.Pop();
                            EvaluationStack.Push(x2);
                        }
                        break;
                    case ScriptOp.OP_OVER:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Peek();
                            EvaluationStack.Push(x2);
                            EvaluationStack.Push(x1);
                        }
                        break;
                    case ScriptOp.OP_PICK:
                        {
                            int n = (int)(BigInteger)EvaluationStack.Pop();
                            if (n < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            EvaluationStack.Push(EvaluationStack.Peek(n));
                        }
                        break;
                    case ScriptOp.OP_ROLL:
                        {
                            int n = (int)(BigInteger)EvaluationStack.Pop();
                            if (n < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            if (n == 0) break;
                            EvaluationStack.Push(EvaluationStack.Remove(n));
                        }
                        break;
                    case ScriptOp.OP_ROT:
                        {
                            StackItem x3 = EvaluationStack.Pop();
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x2);
                            EvaluationStack.Push(x3);
                            EvaluationStack.Push(x1);
                        }
                        break;
                    case ScriptOp.OP_SWAP:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x2);
                            EvaluationStack.Push(x1);
                        }
                        break;
                    case ScriptOp.OP_TUCK:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x2);
                            EvaluationStack.Push(x1);
                            EvaluationStack.Push(x2);
                        }
                        break;
                    case ScriptOp.OP_CAT:
                        {
                            byte[] x2 = (byte[])EvaluationStack.Pop();
                            byte[] x1 = (byte[])EvaluationStack.Pop();
                            EvaluationStack.Push(x1.Concat(x2).ToArray());
                        }
                        break;
                    case ScriptOp.OP_SUBSTR:
                        {
                            int count = (int)(BigInteger)EvaluationStack.Pop();
                            if (count < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            int index = (int)(BigInteger)EvaluationStack.Pop();
                            if (index < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[] x = (byte[])EvaluationStack.Pop();
                            EvaluationStack.Push(x.Skip(index).Take(count).ToArray());
                        }
                        break;
                    case ScriptOp.OP_LEFT:
                        {
                            int count = (int)(BigInteger)EvaluationStack.Pop();
                            if (count < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[] x = (byte[])EvaluationStack.Pop();
                            EvaluationStack.Push(x.Take(count).ToArray());
                        }
                        break;
                    case ScriptOp.OP_RIGHT:
                        {
                            int count = (int)(BigInteger)EvaluationStack.Pop();
                            if (count < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[] x = (byte[])EvaluationStack.Pop();
                            if (x.Length < count)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            EvaluationStack.Push(x.Skip(x.Length - count).ToArray());
                        }
                        break;
                    case ScriptOp.OP_SIZE:
                        {
                            byte[] x = (byte[])EvaluationStack.Peek();
                            EvaluationStack.Push(x.Length);
                        }
                        break;

                    // Bitwise logic
                    case ScriptOp.OP_INVERT:
                        {
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(~x);
                        }
                        break;
                    case ScriptOp.OP_AND:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 & x2);
                        }
                        break;
                    case ScriptOp.OP_OR:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 | x2);
                        }
                        break;
                    case ScriptOp.OP_XOR:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 ^ x2);
                        }
                        break;
                    case ScriptOp.OP_EQUAL:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x1.Equals(x2));
                        }
                        break;

                    // Numeric
                    case ScriptOp.OP_1ADD:
                        {
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x + 1);
                        }
                        break;
                    case ScriptOp.OP_1SUB:
                        {
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x - 1);
                        }
                        break;
                    case ScriptOp.OP_2MUL:
                        {
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x << 1);
                        }
                        break;
                    case ScriptOp.OP_2DIV:
                        {
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x >> 1);
                        }
                        break;
                    case ScriptOp.OP_NEGATE:
                        {
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(-x);
                        }
                        break;
                    case ScriptOp.OP_ABS:
                        {
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(BigInteger.Abs(x));
                        }
                        break;
                    case ScriptOp.OP_NOT:
                        {
                            bool x = EvaluationStack.Pop();
                            EvaluationStack.Push(!x);
                        }
                        break;
                    case ScriptOp.OP_0NOTEQUAL:
                        {
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x != BigInteger.Zero);
                        }
                        break;
                    case ScriptOp.OP_ADD:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 + x2);
                        }
                        break;
                    case ScriptOp.OP_SUB:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 - x2);
                        }
                        break;
                    case ScriptOp.OP_MUL:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 * x2);
                        }
                        break;
                    case ScriptOp.OP_DIV:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 / x2);
                        }
                        break;
                    case ScriptOp.OP_MOD:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 % x2);
                        }
                        break;
                    case ScriptOp.OP_LSHIFT:
                        {
                            int n = (int)(BigInteger)EvaluationStack.Pop();
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x << n);
                        }
                        break;
                    case ScriptOp.OP_RSHIFT:
                        {
                            int n = (int)(BigInteger)EvaluationStack.Pop();
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x >> n);
                        }
                        break;
                    case ScriptOp.OP_BOOLAND:
                        {
                            bool x2 = EvaluationStack.Pop();
                            bool x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x1 && x2);
                        }
                        break;
                    case ScriptOp.OP_BOOLOR:
                        {
                            bool x2 = EvaluationStack.Pop();
                            bool x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x1 || x2);
                        }
                        break;
                    case ScriptOp.OP_NUMEQUAL:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 == x2);
                        }
                        break;
                    case ScriptOp.OP_NUMNOTEQUAL:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 != x2);
                        }
                        break;
                    case ScriptOp.OP_LESSTHAN:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 < x2);
                        }
                        break;
                    case ScriptOp.OP_GREATERTHAN:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 > x2);
                        }
                        break;
                    case ScriptOp.OP_LESSTHANOREQUAL:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 <= x2);
                        }
                        break;
                    case ScriptOp.OP_GREATERTHANOREQUAL:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 >= x2);
                        }
                        break;
                    case ScriptOp.OP_MIN:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(BigInteger.Min(x1, x2));
                        }
                        break;
                    case ScriptOp.OP_MAX:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(BigInteger.Max(x1, x2));
                        }
                        break;
                    case ScriptOp.OP_WITHIN:
                        {
                            BigInteger b = (BigInteger)EvaluationStack.Pop();
                            BigInteger a = (BigInteger)EvaluationStack.Pop();
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(a <= x && x < b);
                        }
                        break;

                    // Crypto
                    case ScriptOp.OP_SHA1:
                        using (SHA1 sha = SHA1.Create())
                        {
                            byte[] x = (byte[])EvaluationStack.Pop();
                            EvaluationStack.Push(sha.ComputeHash(x));
                        }
                        break;
                    case ScriptOp.OP_SHA256:
                        using (SHA256 sha = SHA256.Create())
                        {
                            byte[] x = (byte[])EvaluationStack.Pop();
                            EvaluationStack.Push(sha.ComputeHash(x));
                        }
                        break;
                    case ScriptOp.OP_HASH160:
                        {
                            byte[] x = (byte[])EvaluationStack.Pop();
                            EvaluationStack.Push(Crypto.Hash160(x));
                        }
                        break;
                    case ScriptOp.OP_HASH256:
                        {
                            byte[] x = (byte[])EvaluationStack.Pop();
                            EvaluationStack.Push(Crypto.Hash256(x));
                        }
                        break;
                    case ScriptOp.OP_CHECKSIG:
                        {
                            byte[] pubkey = (byte[])EvaluationStack.Pop();
                            byte[] signature = (byte[])EvaluationStack.Pop();
                            EvaluationStack.Push(Crypto.VerifySignature(ScriptContainer.GetMessage(), signature, pubkey));
                        }
                        break;
                    case ScriptOp.OP_CHECKMULTISIG:
                        {
                            int n = (int)(BigInteger)EvaluationStack.Pop();
                            if (n < 1)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            nOpCount += n;
                            if (nOpCount > max_steps)
                            {
                                State |= VMState.FAULT | VMState.INSUFFICIENT_RESOURCE;
                                return;
                            }
                            byte[][] pubkeys = new byte[n][];
                            for (int i = 0; i < n; i++)
                                pubkeys[i] = (byte[])EvaluationStack.Pop();
                            int m = (int)(BigInteger)EvaluationStack.Pop();
                            if (m < 1 || m > n)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[][] signatures = new byte[m][];
                            for (int i = 0; i < m; i++)
                                signatures[i] = (byte[])EvaluationStack.Pop();
                            byte[] message = ScriptContainer.GetMessage();
                            bool fSuccess = true;
                            for (int i = 0, j = 0; fSuccess && i < m && j < n;)
                            {
                                if (Crypto.VerifySignature(message, signatures[i], pubkeys[j]))
                                    i++;
                                j++;
                                if (m - i > n - j)
                                    fSuccess = false;
                            }
                            EvaluationStack.Push(fSuccess);
                        }
                        break;

                    default:
                        State |= VMState.FAULT;
                        return;
                }
            if (InvocationStack.Count > 0)
            {
                context = InvocationStack.Peek();
                if (context.BreakPoints.Contains((uint)context.InstructionPointer))
                    State |= VMState.BREAK;
            }
        }

        public void LoadScript(byte[] script, bool push_only = false)
        {
            InvocationStack.Push(new ScriptContext(script, push_only));
        }

        public bool RemoveBreakPoint(uint position)
        {
            if (InvocationStack.Count == 0) return false;
            return InvocationStack.Peek().BreakPoints.Remove(position);
        }

        public void StepInto()
        {
            if (InvocationStack.Count == 0) State |= VMState.HALT;
            if (State.HasFlag(VMState.HALT) || State.HasFlag(VMState.FAULT)) return;
            ScriptContext context = InvocationStack.Peek();
            ScriptOp opcode = context.InstructionPointer >= context.Script.Length ? ScriptOp.OP_RET : (ScriptOp)context.OpReader.ReadByte();
            nOpCount++;
            try
            {
                ExecuteOp(opcode, context);
            }
            catch (Exception ex) when (ex is EndOfStreamException || ex is InvalidOperationException)
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
