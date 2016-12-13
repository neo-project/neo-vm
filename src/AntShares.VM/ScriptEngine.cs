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
        private readonly ICrypto crypto;
        private readonly IScriptTable table;
        private readonly IApiService service;
        private int max_steps;

        private int nOpCount = 0;

        public IScriptContainer Signable { get; }
        public RandomAccessStack<ScriptContext> InvocationStack { get; } = new RandomAccessStack<ScriptContext>();
        public RandomAccessStack<StackItem> EvaluationStack { get; } = new RandomAccessStack<StackItem>();
        public RandomAccessStack<StackItem> AltStack { get; } = new RandomAccessStack<StackItem>();
        public byte[] ExecutingScript => InvocationStack.Peek().Script;
        public byte[] CallingScript => InvocationStack.Count > 1 ? InvocationStack.Peek(1).Script : null;
        public VMState State { get; private set; } = VMState.BREAK;

        public ScriptEngine(IScriptContainer signable, ICrypto crypto, int max_steps, IScriptTable table = null, IApiService service = null)
        {
            this.Signable = signable;
            this.crypto = crypto;
            this.table = table;
            this.service = service;
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
            if (opcode > ScriptOp.OP_16 && opcode != ScriptOp.OP_HALT && context.PushOnly)
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
                            int offset = context.InstructionPointer + context.OpReader.ReadInt16() - 3;
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
                        EvaluationStack.Push(context.InstructionPointer + 2);
                        ExecuteOp(ScriptOp.OP_JMP, context);
                        break;
                    case ScriptOp.OP_RET:
                        {
                            StackItem result = EvaluationStack.Pop();
                            int position = (int)(BigInteger)EvaluationStack.Pop();
                            if (position < 0 || position > context.Script.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            EvaluationStack.Push(result);
                            context.InstructionPointer = position;
                        }
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
                        if (service == null || !service.Invoke(Encoding.ASCII.GetString(context.OpReader.ReadVarBytes(252)), this))
                            State |= VMState.FAULT;
                        break;
                    case ScriptOp.OP_HALTIFNOT:
                        if (EvaluationStack.Peek().GetBooleanArray().All(p => p))
                            EvaluationStack.Pop();
                        else
                            ExecuteOp(ScriptOp.OP_HALT, context);
                        break;
                    case ScriptOp.OP_HALT:
                        InvocationStack.Pop().Dispose();
                        if (InvocationStack.Count == 0)
                            State |= VMState.HALT;
                        break;

                    // Stack ops
                    case ScriptOp.OP_TOALTSTACK:
                        AltStack.Push(EvaluationStack.Pop());
                        break;
                    case ScriptOp.OP_FROMALTSTACK:
                        EvaluationStack.Push(AltStack.Pop());
                        break;
                    case ScriptOp.OP_2DROP:
                        EvaluationStack.Pop();
                        EvaluationStack.Pop();
                        break;
                    case ScriptOp.OP_2DUP:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Peek();
                            EvaluationStack.Push(x2);
                            EvaluationStack.Push(x1);
                            EvaluationStack.Push(x2);
                        }
                        break;
                    case ScriptOp.OP_3DUP:
                        {
                            StackItem x3 = EvaluationStack.Pop();
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Peek();
                            EvaluationStack.Push(x2);
                            EvaluationStack.Push(x3);
                            EvaluationStack.Push(x1);
                            EvaluationStack.Push(x2);
                            EvaluationStack.Push(x3);
                        }
                        break;
                    case ScriptOp.OP_2OVER:
                        {
                            StackItem x4 = EvaluationStack.Pop();
                            StackItem x3 = EvaluationStack.Pop();
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Peek();
                            EvaluationStack.Push(x2);
                            EvaluationStack.Push(x3);
                            EvaluationStack.Push(x4);
                            EvaluationStack.Push(x1);
                            EvaluationStack.Push(x2);
                        }
                        break;
                    case ScriptOp.OP_2ROT:
                        {
                            StackItem x6 = EvaluationStack.Pop();
                            StackItem x5 = EvaluationStack.Pop();
                            StackItem x4 = EvaluationStack.Pop();
                            StackItem x3 = EvaluationStack.Pop();
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x3);
                            EvaluationStack.Push(x4);
                            EvaluationStack.Push(x5);
                            EvaluationStack.Push(x6);
                            EvaluationStack.Push(x1);
                            EvaluationStack.Push(x2);
                        }
                        break;
                    case ScriptOp.OP_2SWAP:
                        {
                            StackItem x4 = EvaluationStack.Pop();
                            StackItem x3 = EvaluationStack.Pop();
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x3);
                            EvaluationStack.Push(x4);
                            EvaluationStack.Push(x1);
                            EvaluationStack.Push(x2);
                        }
                        break;
                    case ScriptOp.OP_IFDUP:
                        if (EvaluationStack.Peek())
                            EvaluationStack.Push(EvaluationStack.Peek());
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
                            StackItem[] buffer = new StackItem[n];
                            for (int i = 0; i < n; i++)
                                buffer[i] = EvaluationStack.Pop();
                            StackItem xn = EvaluationStack.Peek();
                            for (int i = n - 1; i >= 0; i--)
                                EvaluationStack.Push(buffer[i]);
                            EvaluationStack.Push(xn);
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
                            StackItem[] buffer = new StackItem[n];
                            for (int i = 0; i < n; i++)
                                buffer[i] = EvaluationStack.Pop();
                            StackItem xn = EvaluationStack.Pop();
                            for (int i = n - 1; i >= 0; i--)
                                EvaluationStack.Push(buffer[i]);
                            EvaluationStack.Push(xn);
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
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            byte[][] b1 = x1.GetBytesArray();
                            byte[][] b2 = x2.GetBytesArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[][] r = b1.Zip(b2, (p1, p2) => p1.Concat(p2).ToArray()).ToArray();
                            EvaluationStack.Push(r);
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
                            StackItem x = EvaluationStack.Pop();
                            byte[][] s = x.GetBytesArray();
                            s = s.Select(p => p.Skip(index).Take(count).ToArray()).ToArray();
                            EvaluationStack.Push(s);
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
                            StackItem x = EvaluationStack.Pop();
                            byte[][] s = x.GetBytesArray();
                            s = s.Select(p => p.Take(count).ToArray()).ToArray();
                            EvaluationStack.Push(s);
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
                            StackItem x = EvaluationStack.Pop();
                            byte[][] s = x.GetBytesArray();
                            if (s.Any(p => p.Length < count))
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            s = s.Select(p => p.Skip(p.Length - count).ToArray()).ToArray();
                            EvaluationStack.Push(s);
                        }
                        break;
                    case ScriptOp.OP_SIZE:
                        {
                            StackItem x = EvaluationStack.Peek();
                            int[] r = x.GetBytesArray().Select(p => p.Length).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;

                    // Bitwise logic
                    case ScriptOp.OP_INVERT:
                        {
                            StackItem x = EvaluationStack.Pop();
                            BigInteger[] r = x.GetIntArray().Select(p => ~p).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_AND:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 & p2).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_OR:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 | p2).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_XOR:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 ^ p2).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_EQUAL:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            byte[][] b1 = x1.GetBytesArray();
                            byte[][] b2 = x2.GetBytesArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool[] r = b1.Zip(b2, (p1, p2) => p1.SequenceEqual(p2)).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;

                    // Numeric
                    case ScriptOp.OP_1ADD:
                        {
                            StackItem x = EvaluationStack.Pop();
                            BigInteger[] r = x.GetIntArray().Select(p => p + BigInteger.One).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_1SUB:
                        {
                            StackItem x = EvaluationStack.Pop();
                            BigInteger[] r = x.GetIntArray().Select(p => p - BigInteger.One).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_2MUL:
                        {
                            StackItem x = EvaluationStack.Pop();
                            BigInteger[] r = x.GetIntArray().Select(p => p << 1).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_2DIV:
                        {
                            StackItem x = EvaluationStack.Pop();
                            BigInteger[] r = x.GetIntArray().Select(p => p >> 1).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_NEGATE:
                        {
                            StackItem x = EvaluationStack.Pop();
                            BigInteger[] r = x.GetIntArray().Select(p => -p).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_ABS:
                        {
                            StackItem x = EvaluationStack.Pop();
                            BigInteger[] r = x.GetIntArray().Select(p => BigInteger.Abs(p)).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_NOT:
                        {
                            StackItem x = EvaluationStack.Pop();
                            bool[] r = x.GetBooleanArray().Select(p => !p).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_0NOTEQUAL:
                        {
                            StackItem x = EvaluationStack.Pop();
                            bool[] r = x.GetIntArray().Select(p => p != BigInteger.Zero).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_ADD:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 + p2).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_SUB:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 - p2).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_MUL:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 * p2).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_DIV:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 / p2).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_MOD:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 % p2).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_LSHIFT:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 << (int)p2).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_RSHIFT:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 >> (int)p2).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_BOOLAND:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            bool[] b1 = x1.GetBooleanArray();
                            bool[] b2 = x2.GetBooleanArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool[] r = b1.Zip(b2, (p1, p2) => p1 && p2).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_BOOLOR:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            bool[] b1 = x1.GetBooleanArray();
                            bool[] b2 = x2.GetBooleanArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool[] r = b1.Zip(b2, (p1, p2) => p1 || p2).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_NUMEQUAL:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool[] r = b1.Zip(b2, (p1, p2) => p1 == p2).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_NUMNOTEQUAL:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool[] r = b1.Zip(b2, (p1, p2) => p1 != p2).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_LESSTHAN:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool[] r = b1.Zip(b2, (p1, p2) => p1 < p2).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_GREATERTHAN:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool[] r = b1.Zip(b2, (p1, p2) => p1 > p2).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_LESSTHANOREQUAL:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool[] r = b1.Zip(b2, (p1, p2) => p1 <= p2).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_GREATERTHANOREQUAL:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool[] r = b1.Zip(b2, (p1, p2) => p1 >= p2).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_MIN:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => BigInteger.Min(p1, p2)).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_MAX:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => BigInteger.Max(p1, p2)).ToArray();
                            EvaluationStack.Push(r);
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
                            StackItem x = EvaluationStack.Pop();
                            byte[][] r = x.GetBytesArray().Select(p => sha.ComputeHash(p)).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_SHA256:
                        using (SHA256 sha = SHA256.Create())
                        {
                            StackItem x = EvaluationStack.Pop();
                            byte[][] r = x.GetBytesArray().Select(p => sha.ComputeHash(p)).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_HASH160:
                        {
                            StackItem x = EvaluationStack.Pop();
                            byte[][] r = x.GetBytesArray().Select(p => crypto.Hash160(p)).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_HASH256:
                        {
                            StackItem x = EvaluationStack.Pop();
                            byte[][] r = x.GetBytesArray().Select(p => crypto.Hash256(p)).ToArray();
                            EvaluationStack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_CHECKSIG:
                        {
                            byte[] pubkey = (byte[])EvaluationStack.Pop();
                            byte[] signature = (byte[])EvaluationStack.Pop();
                            EvaluationStack.Push(crypto.VerifySignature(Signable.GetMessage(), signature, pubkey));
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
                            byte[] message = Signable.GetMessage();
                            bool fSuccess = true;
                            for (int i = 0, j = 0; fSuccess && i < m && j < n;)
                            {
                                if (crypto.VerifySignature(message, signatures[i], pubkeys[j]))
                                    i++;
                                j++;
                                if (m - i > n - j)
                                    fSuccess = false;
                            }
                            EvaluationStack.Push(fSuccess);
                        }
                        break;

                    // Array
                    case ScriptOp.OP_ARRAYSIZE:
                        {
                            StackItem arr = EvaluationStack.Pop();
                            EvaluationStack.Push(arr.Count);
                        }
                        break;
                    case ScriptOp.OP_PACK:
                        {
                            int c = (int)(BigInteger)EvaluationStack.Pop();
                            StackItem[] arr = new StackItem[c];
                            while (c-- > 0)
                                arr[c] = EvaluationStack.Pop();
                            EvaluationStack.Push(new StackItem(arr));
                        }
                        break;
                    case ScriptOp.OP_UNPACK:
                        {
                            StackItem arr = EvaluationStack.Pop();
                            foreach (StackItem item in arr.GetArray())
                                EvaluationStack.Push(item);
                            EvaluationStack.Push(arr.Count);
                        }
                        break;
                    case ScriptOp.OP_DISTINCT:
                        EvaluationStack.Push(EvaluationStack.Pop().Distinct());
                        break;
                    case ScriptOp.OP_SORT:
                        EvaluationStack.Push(EvaluationStack.Pop().GetIntArray().OrderBy(p => p).ToArray());
                        break;
                    case ScriptOp.OP_REVERSE:
                        EvaluationStack.Push(EvaluationStack.Pop().Reverse());
                        break;
                    case ScriptOp.OP_CONCAT:
                        {
                            int c = (int)(BigInteger)EvaluationStack.Pop();
                            if (c == 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            StackItem item = EvaluationStack.Pop();
                            while (--c > 0)
                                item = EvaluationStack.Pop().Concat(item);
                            EvaluationStack.Push(item);
                        }
                        break;
                    case ScriptOp.OP_UNION:
                        {
                            int c = (int)(BigInteger)EvaluationStack.Pop();
                            if (c == 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            StackItem item = EvaluationStack.Pop();
                            while (--c > 0)
                                item = EvaluationStack.Pop().Concat(item);
                            EvaluationStack.Push(item.Distinct());
                        }
                        break;
                    case ScriptOp.OP_INTERSECT:
                        {
                            int c = (int)(BigInteger)EvaluationStack.Pop();
                            if (c == 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            StackItem item = EvaluationStack.Pop();
                            while (--c > 0)
                                item = EvaluationStack.Pop().Intersect(item);
                            EvaluationStack.Push(item);
                        }
                        break;
                    case ScriptOp.OP_EXCEPT:
                        {
                            StackItem x2 = EvaluationStack.Pop();
                            StackItem x1 = EvaluationStack.Pop();
                            EvaluationStack.Push(x1.Except(x2));
                        }
                        break;
                    case ScriptOp.OP_TAKE:
                        {
                            int count = (int)(BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(EvaluationStack.Pop().Take(count));
                        }
                        break;
                    case ScriptOp.OP_SKIP:
                        {
                            int count = (int)(BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(EvaluationStack.Pop().Skip(count));
                        }
                        break;
                    case ScriptOp.OP_PICKITEM:
                        {
                            int index = (int)(BigInteger)EvaluationStack.Pop();
                            EvaluationStack.Push(EvaluationStack.Pop().ElementAt(index));
                        }
                        break;
                    case ScriptOp.OP_ALL:
                        EvaluationStack.Push(EvaluationStack.Pop().GetBooleanArray().All(p => p));
                        break;
                    case ScriptOp.OP_ANY:
                        EvaluationStack.Push(EvaluationStack.Pop().GetBooleanArray().Any(p => p));
                        break;
                    case ScriptOp.OP_SUM:
                        EvaluationStack.Push(EvaluationStack.Pop().GetIntArray().Aggregate(BigInteger.Zero, (s, p) => s + p));
                        break;
                    case ScriptOp.OP_AVERAGE:
                        {
                            StackItem arr = EvaluationStack.Pop();
                            if (arr.Count == 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            EvaluationStack.Push(arr.GetIntArray().Aggregate(BigInteger.Zero, (s, p) => s + p, p => p / arr.Count));
                        }
                        break;
                    case ScriptOp.OP_MAXITEM:
                        EvaluationStack.Push(EvaluationStack.Pop().GetIntArray().Max());
                        break;
                    case ScriptOp.OP_MINITEM:
                        EvaluationStack.Push(EvaluationStack.Pop().GetIntArray().Min());
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
            ScriptOp opcode = context.InstructionPointer >= context.Script.Length ? ScriptOp.OP_HALT : (ScriptOp)context.OpReader.ReadByte();
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
