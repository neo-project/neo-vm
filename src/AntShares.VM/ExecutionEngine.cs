using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace AntShares.VM
{
    public class ExecutionEngine : IDisposable
    {
        private readonly IScriptTable table;
        private readonly ApiService service;
        private int max_steps;

        private int nOpCount = 0;

        public IScriptContainer ScriptContainer { get; }
        public ICrypto Crypto { get; }
        public RandomAccessStack<ExecutionContext> InvocationStack { get; } = new RandomAccessStack<ExecutionContext>();
        public RandomAccessStack<StackItem> EvaluationStack { get; } = new RandomAccessStack<StackItem>();
        public RandomAccessStack<StackItem> AltStack { get; } = new RandomAccessStack<StackItem>();
        public byte[] ExecutingScript => InvocationStack.Peek().Script;
        public byte[] CallingScript => InvocationStack.Count > 1 ? InvocationStack.Peek(1).Script : null;
        public byte[] EntryScript => InvocationStack.Peek(InvocationStack.Count - 1).Script;
        public VMState State { get; private set; } = VMState.BREAK;

        public ExecutionEngine(IScriptContainer container, ICrypto crypto, int max_steps, IScriptTable table = null, ApiService service = null)
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

        private void ExecuteOp(OpCode opcode, ExecutionContext context)
        {
            if (opcode > OpCode.OP_16 && opcode != OpCode.OP_RET && context.PushOnly)
            {
                State |= VMState.FAULT;
                return;
            }
            if (opcode > OpCode.OP_16 && nOpCount > max_steps)
            {
                State |= VMState.FAULT | VMState.INSUFFICIENT_RESOURCE;
                return;
            }
            if (opcode >= OpCode.OP_PUSHBYTES1 && opcode <= OpCode.OP_PUSHBYTES75)
                EvaluationStack.Push(context.OpReader.ReadBytes((byte)opcode));
            else
                switch (opcode)
                {
                    // Push value
                    case OpCode.OP_0:
                        EvaluationStack.Push(new byte[0]);
                        break;
                    case OpCode.OP_PUSHDATA1:
                        EvaluationStack.Push(context.OpReader.ReadBytes(context.OpReader.ReadByte()));
                        break;
                    case OpCode.OP_PUSHDATA2:
                        EvaluationStack.Push(context.OpReader.ReadBytes(context.OpReader.ReadUInt16()));
                        break;
                    case OpCode.OP_PUSHDATA4:
                        EvaluationStack.Push(context.OpReader.ReadBytes(context.OpReader.ReadInt32()));
                        break;
                    case OpCode.OP_1NEGATE:
                    case OpCode.OP_1:
                    case OpCode.OP_2:
                    case OpCode.OP_3:
                    case OpCode.OP_4:
                    case OpCode.OP_5:
                    case OpCode.OP_6:
                    case OpCode.OP_7:
                    case OpCode.OP_8:
                    case OpCode.OP_9:
                    case OpCode.OP_10:
                    case OpCode.OP_11:
                    case OpCode.OP_12:
                    case OpCode.OP_13:
                    case OpCode.OP_14:
                    case OpCode.OP_15:
                    case OpCode.OP_16:
                        EvaluationStack.Push(opcode - OpCode.OP_1 + 1);
                        break;

                    // Control
                    case OpCode.OP_NOP:
                        break;
                    case OpCode.OP_JMP:
                    case OpCode.OP_JMPIF:
                    case OpCode.OP_JMPIFNOT:
                        {
                            int offset = context.OpReader.ReadInt16();
                            offset = context.InstructionPointer + offset - 3;
                            if (offset < 0 || offset > context.Script.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool fValue = true;
                            if (opcode > OpCode.OP_JMP)
                            {
                                fValue = EvaluationStack.Pop();
                                if (opcode == OpCode.OP_JMPIFNOT)
                                    fValue = !fValue;
                            }
                            if (fValue)
                                context.InstructionPointer = offset;
                        }
                        break;
                    case OpCode.OP_CALL:
                        InvocationStack.Push(context.Clone());
                        context.InstructionPointer += 2;
                        ExecuteOp(OpCode.OP_JMP, InvocationStack.Peek());
                        break;
                    case OpCode.OP_RET:
                        InvocationStack.Pop().Dispose();
                        if (InvocationStack.Count == 0)
                            State |= VMState.HALT;
                        break;
                    case OpCode.OP_APPCALL:
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
                    case OpCode.OP_SYSCALL:
                        if (!service.Invoke(Encoding.ASCII.GetString(context.OpReader.ReadVarBytes(252)), this))
                            State |= VMState.FAULT;
                        break;

                    // Stack ops
                    case OpCode.OP_TOALTSTACK:
                        AltStack.Push(EvaluationStack.Pop());
                        break;
                    case OpCode.OP_FROMALTSTACK:
                        EvaluationStack.Push(AltStack.Pop());
                        break;
                    case OpCode.OP_XDROP:
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
                    case OpCode.OP_XSWAP:
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
                    case OpCode.OP_XTUCK:
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
                    case OpCode.OP_DEPTH:
                        EvaluationStack.Push(EvaluationStack.Count);
                        break;
                    case OpCode.OP_DROP:
                        EvaluationStack.Pop();
                        break;
                    case OpCode.OP_DUP:
                        EvaluationStack.Push(EvaluationStack.Peek());
                        break;
                    case OpCode.OP_NIP:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            EvaluationStack.Pop();
                            EvaluationStack.Push(x2);
                        }
                        break;
                    case OpCode.OP_OVER:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Peek();
                            EvaluationStack.Push(x2);
                            EvaluationStack.Push(x1);
                        }
                        break;
                    case OpCode.OP_PICK:
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
                    case OpCode.OP_ROLL:
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
                    case OpCode.OP_ROT:
                        {
                            StackItem x3 = EvaluationStack.Pop();
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x2);
                            EvaluationStack.Push(x3);
                            EvaluationStack.Push(x1);
                        }
                        break;
                    case OpCode.OP_SWAP:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x2);
                            EvaluationStack.Push(x1);
                        }
                        break;
                    case OpCode.OP_TUCK:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x2);
                            EvaluationStack.Push(x1);
                            EvaluationStack.Push(x2);
                        }
                        break;
                    case OpCode.OP_CAT:
                        {
                            byte[] x2 = (byte[])EvaluationStack.Pop();
                            byte[] x1 = (byte[])EvaluationStack.Pop();
                            EvaluationStack.Push(x1.Concat(x2).ToArray());
                        }
                        break;
                    case OpCode.OP_SUBSTR:
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
                    case OpCode.OP_LEFT:
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
                    case OpCode.OP_RIGHT:
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
                    case OpCode.OP_SIZE:
                        {
                            byte[] x = (byte[])EvaluationStack.Peek();
                            EvaluationStack.Push(x.Length);
                        }
                        break;

                    // Bitwise logic
                    case OpCode.OP_INVERT:
                        {
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(~x);
                        }
                        break;
                    case OpCode.OP_AND:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 & x2);
                        }
                        break;
                    case OpCode.OP_OR:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 | x2);
                        }
                        break;
                    case OpCode.OP_XOR:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 ^ x2);
                        }
                        break;
                    case OpCode.OP_EQUAL:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x1.Equals(x2));
                        }
                        break;

                    // Numeric
                    case OpCode.OP_1ADD:
                        {
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x + 1);
                        }
                        break;
                    case OpCode.OP_1SUB:
                        {
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x - 1);
                        }
                        break;
                    case OpCode.OP_2MUL:
                        {
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x << 1);
                        }
                        break;
                    case OpCode.OP_2DIV:
                        {
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x >> 1);
                        }
                        break;
                    case OpCode.OP_NEGATE:
                        {
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(-x);
                        }
                        break;
                    case OpCode.OP_ABS:
                        {
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(BigInteger.Abs(x));
                        }
                        break;
                    case OpCode.OP_NOT:
                        {
                            bool x = EvaluationStack.Pop();
                            EvaluationStack.Push(!x);
                        }
                        break;
                    case OpCode.OP_0NOTEQUAL:
                        {
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x != BigInteger.Zero);
                        }
                        break;
                    case OpCode.OP_ADD:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 + x2);
                        }
                        break;
                    case OpCode.OP_SUB:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 - x2);
                        }
                        break;
                    case OpCode.OP_MUL:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 * x2);
                        }
                        break;
                    case OpCode.OP_DIV:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 / x2);
                        }
                        break;
                    case OpCode.OP_MOD:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 % x2);
                        }
                        break;
                    case OpCode.OP_LSHIFT:
                        {
                            int n = (int)(BigInteger)EvaluationStack.Pop();
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x << n);
                        }
                        break;
                    case OpCode.OP_RSHIFT:
                        {
                            int n = (int)(BigInteger)EvaluationStack.Pop();
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x >> n);
                        }
                        break;
                    case OpCode.OP_BOOLAND:
                        {
                            bool x2 = EvaluationStack.Pop();
                            bool x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x1 && x2);
                        }
                        break;
                    case OpCode.OP_BOOLOR:
                        {
                            bool x2 = EvaluationStack.Pop();
                            bool x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x1 || x2);
                        }
                        break;
                    case OpCode.OP_NUMEQUAL:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 == x2);
                        }
                        break;
                    case OpCode.OP_NUMNOTEQUAL:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 != x2);
                        }
                        break;
                    case OpCode.OP_LESSTHAN:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 < x2);
                        }
                        break;
                    case OpCode.OP_GREATERTHAN:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 > x2);
                        }
                        break;
                    case OpCode.OP_LESSTHANOREQUAL:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 <= x2);
                        }
                        break;
                    case OpCode.OP_GREATERTHANOREQUAL:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(x1 >= x2);
                        }
                        break;
                    case OpCode.OP_MIN:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(BigInteger.Min(x1, x2));
                        }
                        break;
                    case OpCode.OP_MAX:
                        {
                            BigInteger x2 = (BigInteger)EvaluationStack.Pop();
                            BigInteger x1 = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(BigInteger.Max(x1, x2));
                        }
                        break;
                    case OpCode.OP_WITHIN:
                        {
                            BigInteger b = (BigInteger)EvaluationStack.Pop();
                            BigInteger a = (BigInteger)EvaluationStack.Pop();
                            BigInteger x = (BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(a <= x && x < b);
                        }
                        break;

                    // Crypto
                    case OpCode.OP_SHA1:
                        using (SHA1 sha = SHA1.Create())
                        {
                            byte[] x = (byte[])EvaluationStack.Pop();
                            EvaluationStack.Push(sha.ComputeHash(x));
                        }
                        break;
                    case OpCode.OP_SHA256:
                        using (SHA256 sha = SHA256.Create())
                        {
                            byte[] x = (byte[])EvaluationStack.Pop();
                            EvaluationStack.Push(sha.ComputeHash(x));
                        }
                        break;
                    case OpCode.OP_HASH160:
                        {
                            byte[] x = (byte[])EvaluationStack.Pop();
                            EvaluationStack.Push(Crypto.Hash160(x));
                        }
                        break;
                    case OpCode.OP_HASH256:
                        {
                            byte[] x = (byte[])EvaluationStack.Pop();
                            EvaluationStack.Push(Crypto.Hash256(x));
                        }
                        break;
                    case OpCode.OP_CHECKSIG:
                        {
                            byte[] pubkey = (byte[])EvaluationStack.Pop();
                            byte[] signature = (byte[])EvaluationStack.Pop();
                            EvaluationStack.Push(Crypto.VerifySignature(ScriptContainer.GetMessage(), signature, pubkey));
                        }
                        break;
                    case OpCode.OP_CHECKMULTISIG:
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
            InvocationStack.Push(new ExecutionContext(script, push_only));
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
            ExecutionContext context = InvocationStack.Peek();
            OpCode opcode = context.InstructionPointer >= context.Script.Length ? OpCode.OP_RET : (OpCode)context.OpReader.ReadByte();
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
