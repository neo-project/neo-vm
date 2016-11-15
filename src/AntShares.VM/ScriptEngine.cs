using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;

namespace AntShares.VM
{
    public class ScriptEngine : IDisposable
    {
        private readonly ICrypto crypto;
        private readonly IScriptTable table;
        private readonly IApiService service;
        private int max_steps;

        private Stack<ScriptContext> script_stack = new Stack<ScriptContext>();
        private int nOpCount = 0;

        public ISignableObject Signable { get; }
        public Stack<StackItem> Stack { get; } = new Stack<StackItem>();
        public Stack<StackItem> AltStack { get; } = new Stack<StackItem>();
        public byte[] ExecutingScript => script_stack.Peek().Script;
        public VMState State { get; private set; } = VMState.BREAK;
        public bool PushOnly { get; set; }

        public ScriptEngine(ISignableObject signable, ICrypto crypto, int max_steps, IScriptTable table = null, IApiService service = null)
        {
            this.Signable = signable;
            this.crypto = crypto;
            this.table = table;
            this.service = service;
            this.max_steps = max_steps;
        }

        public void AddBreakPoint(uint position)
        {
            script_stack.Peek().BreakPoints.Add(position);
        }

        public void Dispose()
        {
            while (script_stack.Count > 0)
                script_stack.Pop().Dispose();
        }

        public void Execute()
        {
            State &= ~VMState.BREAK;
            while (!State.HasFlag(VMState.HALT) && !State.HasFlag(VMState.FAULT) && !State.HasFlag(VMState.BREAK))
                ExecuteStep();
        }

        public void ExecuteStep()
        {
            if (script_stack.Count == 0) State |= VMState.HALT;
            if (State.HasFlag(VMState.HALT) || State.HasFlag(VMState.FAULT)) return;
            BinaryReader opReader = script_stack.Peek().OpReader;
            ScriptOp opcode = opReader.BaseStream.Position >= opReader.BaseStream.Length ? ScriptOp.OP_HALT : (ScriptOp)opReader.ReadByte();
            nOpCount++;
            try
            {
                ExecuteOp(opcode, opReader);
            }
            catch (Exception ex) when (ex is EndOfStreamException || ex is InvalidOperationException)
            {
                State |= VMState.FAULT;
            }
        }

        private void ExecuteOp(ScriptOp opcode, BinaryReader opReader)
        {
            if (opcode > ScriptOp.OP_16 && opcode != ScriptOp.OP_HALT && PushOnly)
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
                Stack.Push(opReader.ReadBytes((byte)opcode));
            else
                switch (opcode)
                {
                    // Push value
                    case ScriptOp.OP_0:
                        Stack.Push(new byte[0]);
                        break;
                    case ScriptOp.OP_PUSHDATA1:
                        Stack.Push(opReader.ReadBytes(opReader.ReadByte()));
                        break;
                    case ScriptOp.OP_PUSHDATA2:
                        Stack.Push(opReader.ReadBytes(opReader.ReadUInt16()));
                        break;
                    case ScriptOp.OP_PUSHDATA4:
                        Stack.Push(opReader.ReadBytes(opReader.ReadInt32()));
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
                        Stack.Push(opcode - ScriptOp.OP_1 + 1);
                        break;

                    // Control
                    case ScriptOp.OP_NOP:
                        break;
                    case ScriptOp.OP_JMP:
                    case ScriptOp.OP_JMPIF:
                    case ScriptOp.OP_JMPIFNOT:
                        {
                            int offset = (int)opReader.BaseStream.Position + opReader.ReadInt16() - 3;
                            if (offset < 0 || offset > opReader.BaseStream.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool fValue = true;
                            if (opcode > ScriptOp.OP_JMP)
                            {
                                fValue = Stack.Pop();
                                if (opcode == ScriptOp.OP_JMPIFNOT)
                                    fValue = !fValue;
                            }
                            if (fValue)
                                opReader.BaseStream.Seek(offset, SeekOrigin.Begin);
                        }
                        break;
                    case ScriptOp.OP_CALL:
                        Stack.Push(opReader.BaseStream.Position + 2);
                        ExecuteOp(ScriptOp.OP_JMP, opReader);
                        break;
                    case ScriptOp.OP_RET:
                        {
                            StackItem result = Stack.Pop();
                            int position = (int)(BigInteger)Stack.Pop();
                            if (position < 0 || position > opReader.BaseStream.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            Stack.Push(result);
                            opReader.BaseStream.Seek(position, SeekOrigin.Begin);
                        }
                        break;
                    case ScriptOp.OP_APPCALL:
                        {
                            if (table == null)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[] script_hash = opReader.ReadBytes(20);
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
                        if (service == null || !service.Invoke(opReader.ReadVarString(), this))
                            State |= VMState.FAULT;
                        break;
                    case ScriptOp.OP_HALTIFNOT:
                        if (Stack.Peek().GetBooleanArray().All(p => p))
                            Stack.Pop();
                        else
                            ExecuteOp(ScriptOp.OP_HALT, opReader);
                        break;
                    case ScriptOp.OP_HALT:
                        script_stack.Pop().Dispose();
                        if (script_stack.Count == 0)
                            State |= VMState.HALT;
                        break;

                    // Stack ops
                    case ScriptOp.OP_TOALTSTACK:
                        AltStack.Push(Stack.Pop());
                        break;
                    case ScriptOp.OP_FROMALTSTACK:
                        Stack.Push(AltStack.Pop());
                        break;
                    case ScriptOp.OP_2DROP:
                        Stack.Pop();
                        Stack.Pop();
                        break;
                    case ScriptOp.OP_2DUP:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Peek();
                            Stack.Push(x2);
                            Stack.Push(x1);
                            Stack.Push(x2);
                        }
                        break;
                    case ScriptOp.OP_3DUP:
                        {
                            StackItem x3 = Stack.Pop();
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Peek();
                            Stack.Push(x2);
                            Stack.Push(x3);
                            Stack.Push(x1);
                            Stack.Push(x2);
                            Stack.Push(x3);
                        }
                        break;
                    case ScriptOp.OP_2OVER:
                        {
                            StackItem x4 = Stack.Pop();
                            StackItem x3 = Stack.Pop();
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Peek();
                            Stack.Push(x2);
                            Stack.Push(x3);
                            Stack.Push(x4);
                            Stack.Push(x1);
                            Stack.Push(x2);
                        }
                        break;
                    case ScriptOp.OP_2ROT:
                        {
                            StackItem x6 = Stack.Pop();
                            StackItem x5 = Stack.Pop();
                            StackItem x4 = Stack.Pop();
                            StackItem x3 = Stack.Pop();
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            Stack.Push(x3);
                            Stack.Push(x4);
                            Stack.Push(x5);
                            Stack.Push(x6);
                            Stack.Push(x1);
                            Stack.Push(x2);
                        }
                        break;
                    case ScriptOp.OP_2SWAP:
                        {
                            StackItem x4 = Stack.Pop();
                            StackItem x3 = Stack.Pop();
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            Stack.Push(x3);
                            Stack.Push(x4);
                            Stack.Push(x1);
                            Stack.Push(x2);
                        }
                        break;
                    case ScriptOp.OP_IFDUP:
                        if (Stack.Peek())
                            Stack.Push(Stack.Peek());
                        break;
                    case ScriptOp.OP_DEPTH:
                        Stack.Push(Stack.Count);
                        break;
                    case ScriptOp.OP_DROP:
                        Stack.Pop();
                        break;
                    case ScriptOp.OP_DUP:
                        Stack.Push(Stack.Peek());
                        break;
                    case ScriptOp.OP_NIP:
                        {
                            StackItem x2 = Stack.Pop();
                            Stack.Pop();
                            Stack.Push(x2);
                        }
                        break;
                    case ScriptOp.OP_OVER:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Peek();
                            Stack.Push(x2);
                            Stack.Push(x1);
                        }
                        break;
                    case ScriptOp.OP_PICK:
                        {
                            int n = (int)(BigInteger)Stack.Pop();
                            if (n < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            StackItem[] buffer = new StackItem[n];
                            for (int i = 0; i < n; i++)
                                buffer[i] = Stack.Pop();
                            StackItem xn = Stack.Peek();
                            for (int i = n - 1; i >= 0; i--)
                                Stack.Push(buffer[i]);
                            Stack.Push(xn);
                        }
                        break;
                    case ScriptOp.OP_ROLL:
                        {
                            int n = (int)(BigInteger)Stack.Pop();
                            if (n < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            if (n == 0) break;
                            StackItem[] buffer = new StackItem[n];
                            for (int i = 0; i < n; i++)
                                buffer[i] = Stack.Pop();
                            StackItem xn = Stack.Pop();
                            for (int i = n - 1; i >= 0; i--)
                                Stack.Push(buffer[i]);
                            Stack.Push(xn);
                        }
                        break;
                    case ScriptOp.OP_ROT:
                        {
                            StackItem x3 = Stack.Pop();
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            Stack.Push(x2);
                            Stack.Push(x3);
                            Stack.Push(x1);
                        }
                        break;
                    case ScriptOp.OP_SWAP:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            Stack.Push(x2);
                            Stack.Push(x1);
                        }
                        break;
                    case ScriptOp.OP_TUCK:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            Stack.Push(x2);
                            Stack.Push(x1);
                            Stack.Push(x2);
                        }
                        break;
                    case ScriptOp.OP_CAT:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            byte[][] b1 = x1.GetBytesArray();
                            byte[][] b2 = x2.GetBytesArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[][] r = b1.Zip(b2, (p1, p2) => p1.Concat(p2).ToArray()).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_SUBSTR:
                        {
                            int count = (int)(BigInteger)Stack.Pop();
                            if (count < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            int index = (int)(BigInteger)Stack.Pop();
                            if (index < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            StackItem x = Stack.Pop();
                            byte[][] s = x.GetBytesArray();
                            s = s.Select(p => p.Skip(index).Take(count).ToArray()).ToArray();
                            Stack.Push(s);
                        }
                        break;
                    case ScriptOp.OP_LEFT:
                        {
                            int count = (int)(BigInteger)Stack.Pop();
                            if (count < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            StackItem x = Stack.Pop();
                            byte[][] s = x.GetBytesArray();
                            s = s.Select(p => p.Take(count).ToArray()).ToArray();
                            Stack.Push(s);
                        }
                        break;
                    case ScriptOp.OP_RIGHT:
                        {
                            int count = (int)(BigInteger)Stack.Pop();
                            if (count < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            StackItem x = Stack.Pop();
                            byte[][] s = x.GetBytesArray();
                            if (s.Any(p => p.Length < count))
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            s = s.Select(p => p.Skip(p.Length - count).ToArray()).ToArray();
                            Stack.Push(s);
                        }
                        break;
                    case ScriptOp.OP_SIZE:
                        {
                            StackItem x = Stack.Peek();
                            int[] r = x.GetBytesArray().Select(p => p.Length).ToArray();
                            Stack.Push(r);
                        }
                        break;

                    // Bitwise logic
                    case ScriptOp.OP_INVERT:
                        {
                            StackItem x = Stack.Pop();
                            BigInteger[] r = x.GetIntArray().Select(p => ~p).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_AND:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 & p2).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_OR:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 | p2).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_XOR:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 ^ p2).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_EQUAL:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            byte[][] b1 = x1.GetBytesArray();
                            byte[][] b2 = x2.GetBytesArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool[] r = b1.Zip(b2, (p1, p2) => p1.SequenceEqual(p2)).ToArray();
                            Stack.Push(r);
                        }
                        break;

                    // Numeric
                    case ScriptOp.OP_1ADD:
                        {
                            StackItem x = Stack.Pop();
                            BigInteger[] r = x.GetIntArray().Select(p => p + BigInteger.One).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_1SUB:
                        {
                            StackItem x = Stack.Pop();
                            BigInteger[] r = x.GetIntArray().Select(p => p - BigInteger.One).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_2MUL:
                        {
                            StackItem x = Stack.Pop();
                            BigInteger[] r = x.GetIntArray().Select(p => p << 1).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_2DIV:
                        {
                            StackItem x = Stack.Pop();
                            BigInteger[] r = x.GetIntArray().Select(p => p >> 1).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_NEGATE:
                        {
                            StackItem x = Stack.Pop();
                            BigInteger[] r = x.GetIntArray().Select(p => -p).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_ABS:
                        {
                            StackItem x = Stack.Pop();
                            BigInteger[] r = x.GetIntArray().Select(p => BigInteger.Abs(p)).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_NOT:
                        {
                            StackItem x = Stack.Pop();
                            bool[] r = x.GetBooleanArray().Select(p => !p).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_0NOTEQUAL:
                        {
                            StackItem x = Stack.Pop();
                            bool[] r = x.GetIntArray().Select(p => p != BigInteger.Zero).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_ADD:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 + p2).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_SUB:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 - p2).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_MUL:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 * p2).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_DIV:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 / p2).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_MOD:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 % p2).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_LSHIFT:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 << (int)p2).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_RSHIFT:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 >> (int)p2).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_BOOLAND:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            bool[] b1 = x1.GetBooleanArray();
                            bool[] b2 = x2.GetBooleanArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool[] r = b1.Zip(b2, (p1, p2) => p1 && p2).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_BOOLOR:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            bool[] b1 = x1.GetBooleanArray();
                            bool[] b2 = x2.GetBooleanArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool[] r = b1.Zip(b2, (p1, p2) => p1 || p2).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_NUMEQUAL:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool[] r = b1.Zip(b2, (p1, p2) => p1 == p2).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_NUMNOTEQUAL:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool[] r = b1.Zip(b2, (p1, p2) => p1 != p2).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_LESSTHAN:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool[] r = b1.Zip(b2, (p1, p2) => p1 < p2).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_GREATERTHAN:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool[] r = b1.Zip(b2, (p1, p2) => p1 > p2).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_LESSTHANOREQUAL:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool[] r = b1.Zip(b2, (p1, p2) => p1 <= p2).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_GREATERTHANOREQUAL:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            bool[] r = b1.Zip(b2, (p1, p2) => p1 >= p2).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_MIN:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => BigInteger.Min(p1, p2)).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_MAX:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            BigInteger[] b1 = x1.GetIntArray();
                            BigInteger[] b2 = x2.GetIntArray();
                            if (b1.Length != b2.Length)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            BigInteger[] r = b1.Zip(b2, (p1, p2) => BigInteger.Max(p1, p2)).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_WITHIN:
                        {
                            BigInteger b = (BigInteger)Stack.Pop();
                            BigInteger a = (BigInteger)Stack.Pop();
                            BigInteger x = (BigInteger)Stack.Pop();
                            Stack.Push(a <= x && x < b);
                        }
                        break;

                    // Crypto
                    case ScriptOp.OP_SHA1:
                        using (SHA1 sha = SHA1.Create())
                        {
                            StackItem x = Stack.Pop();
                            byte[][] r = x.GetBytesArray().Select(p => sha.ComputeHash(p)).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_SHA256:
                        using (SHA256 sha = SHA256.Create())
                        {
                            StackItem x = Stack.Pop();
                            byte[][] r = x.GetBytesArray().Select(p => sha.ComputeHash(p)).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_HASH160:
                        {
                            StackItem x = Stack.Pop();
                            byte[][] r = x.GetBytesArray().Select(p => crypto.Hash160(p)).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_HASH256:
                        {
                            StackItem x = Stack.Pop();
                            byte[][] r = x.GetBytesArray().Select(p => crypto.Hash256(p)).ToArray();
                            Stack.Push(r);
                        }
                        break;
                    case ScriptOp.OP_CHECKSIG:
                        {
                            byte[] pubkey = (byte[])Stack.Pop();
                            byte[] signature = (byte[])Stack.Pop();
                            Stack.Push(crypto.VerifySignature(Signable.GetMessage(), signature, pubkey));
                        }
                        break;
                    case ScriptOp.OP_CHECKMULTISIG:
                        {
                            int n = (int)(BigInteger)Stack.Pop();
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
                                pubkeys[i] = (byte[])Stack.Pop();
                            int m = (int)(BigInteger)Stack.Pop();
                            if (m < 1 || m > n)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[][] signatures = new byte[m][];
                            for (int i = 0; i < m; i++)
                                signatures[i] = (byte[])Stack.Pop();
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
                            Stack.Push(fSuccess);
                        }
                        break;

                    // Array
                    case ScriptOp.OP_ARRAYSIZE:
                        {
                            StackItem arr = Stack.Pop();
                            Stack.Push(arr.Count);
                        }
                        break;
                    case ScriptOp.OP_PACK:
                        {
                            int c = (int)(BigInteger)Stack.Pop();
                            StackItem[] arr = new StackItem[c];
                            while (c-- > 0)
                                arr[c] = Stack.Pop();
                            Stack.Push(new StackItem(arr));
                        }
                        break;
                    case ScriptOp.OP_UNPACK:
                        {
                            StackItem arr = Stack.Pop();
                            foreach (StackItem item in arr.GetArray())
                                Stack.Push(item);
                            Stack.Push(arr.Count);
                        }
                        break;
                    case ScriptOp.OP_DISTINCT:
                        Stack.Push(Stack.Pop().Distinct());
                        break;
                    case ScriptOp.OP_SORT:
                        Stack.Push(Stack.Pop().GetIntArray().OrderBy(p => p).ToArray());
                        break;
                    case ScriptOp.OP_REVERSE:
                        Stack.Push(Stack.Pop().Reverse());
                        break;
                    case ScriptOp.OP_CONCAT:
                        {
                            int c = (int)(BigInteger)Stack.Pop();
                            if (c == 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            StackItem item = Stack.Pop();
                            while (--c > 0)
                                item = Stack.Pop().Concat(item);
                            Stack.Push(item);
                        }
                        break;
                    case ScriptOp.OP_UNION:
                        {
                            int c = (int)(BigInteger)Stack.Pop();
                            if (c == 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            StackItem item = Stack.Pop();
                            while (--c > 0)
                                item = Stack.Pop().Concat(item);
                            Stack.Push(item.Distinct());
                        }
                        break;
                    case ScriptOp.OP_INTERSECT:
                        {
                            int c = (int)(BigInteger)Stack.Pop();
                            if (c == 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            StackItem item = Stack.Pop();
                            while (--c > 0)
                                item = Stack.Pop().Intersect(item);
                            Stack.Push(item);
                        }
                        break;
                    case ScriptOp.OP_EXCEPT:
                        {
                            StackItem x2 = Stack.Pop();
                            StackItem x1 = Stack.Pop();
                            Stack.Push(x1.Except(x2));
                        }
                        break;
                    case ScriptOp.OP_TAKE:
                        {
                            int count = (int)(BigInteger)Stack.Pop();
                            Stack.Push(Stack.Pop().Take(count));
                        }
                        break;
                    case ScriptOp.OP_SKIP:
                        {
                            int count = (int)(BigInteger)Stack.Pop();
                            Stack.Push(Stack.Pop().Skip(count));
                        }
                        break;
                    case ScriptOp.OP_PICKITEM:
                        {
                            int index = (int)(BigInteger)Stack.Pop();
                            Stack.Push(Stack.Pop().ElementAt(index));
                        }
                        break;
                    case ScriptOp.OP_ALL:
                        Stack.Push(Stack.Pop().GetBooleanArray().All(p => p));
                        break;
                    case ScriptOp.OP_ANY:
                        Stack.Push(Stack.Pop().GetBooleanArray().Any(p => p));
                        break;
                    case ScriptOp.OP_SUM:
                        Stack.Push(Stack.Pop().GetIntArray().Aggregate(BigInteger.Zero, (s, p) => s + p));
                        break;
                    case ScriptOp.OP_AVERAGE:
                        {
                            StackItem arr = Stack.Pop();
                            if (arr.Count == 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            Stack.Push(arr.GetIntArray().Aggregate(BigInteger.Zero, (s, p) => s + p, p => p / arr.Count));
                        }
                        break;
                    case ScriptOp.OP_MAXITEM:
                        Stack.Push(Stack.Pop().GetIntArray().Max());
                        break;
                    case ScriptOp.OP_MINITEM:
                        Stack.Push(Stack.Pop().GetIntArray().Min());
                        break;

                    default:
                        State |= VMState.FAULT;
                        return;
                }
            if (script_stack.Peek().BreakPoints.Contains((uint)opReader.BaseStream.Position))
                State |= VMState.BREAK;
        }

        public void LoadScript(byte[] script)
        {
            script_stack.Push(new ScriptContext(script));
        }

        public bool RemoveBreakPoint(uint position)
        {
            if (script_stack.Count == 0) return false;
            return script_stack.Peek().BreakPoints.Remove(position);
        }
    }
}
