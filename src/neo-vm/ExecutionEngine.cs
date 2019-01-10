using Neo.VM.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using VMArray = Neo.VM.Types.Array;

namespace Neo.VM
{
    public class ExecutionEngine : IDisposable
    {
        internal int stackitem_count = 0;
        internal bool is_stackitem_count_strict = true;

        private readonly IScriptTable table;
        private Dictionary<byte[], HashSet<uint>> break_points;

        public IScriptContainer ScriptContainer { get; }
        public ICrypto Crypto { get; }
        public VMLimits Limits { get; }
        public IInteropService Service { get; }
        public RandomAccessStack<ExecutionContext> InvocationStack { get; } = new RandomAccessStack<ExecutionContext>();
        public RandomAccessStack<StackItem> ResultStack { get; } = new RandomAccessStack<StackItem>();
        public ExecutionContext CurrentContext => InvocationStack.Peek();
        public ExecutionContext CallingContext => InvocationStack.Count > 1 ? InvocationStack.Peek(1) : null;
        public ExecutionContext EntryContext => InvocationStack.Peek(InvocationStack.Count - 1);
        public VMState State { get; protected set; } = VMState.BREAK;

        public ExecutionEngine(IScriptContainer container, ICrypto crypto, IScriptTable table = null, IInteropService service = null, VMLimits limits = null)
        {
            this.ScriptContainer = container;
            this.Crypto = crypto;
            this.table = table;
            this.Service = service;
            this.Limits = limits ?? VMLimits.Default;
        }

        public void AddBreakPoint(byte[] script_hash, uint position)
        {
            if (break_points == null)
            {
                break_points = new Dictionary<byte[], HashSet<uint>>(new HashComparer());
            }

            if (!break_points.TryGetValue(script_hash, out HashSet<uint> hashset))
            {
                hashset = new HashSet<uint>();
                break_points.Add(script_hash, hashset);
            }
            hashset.Add(position);
        }

        public bool RemoveBreakPoint(byte[] script_hash, uint position)
        {
            if (break_points == null)
                return false;
            if (!break_points.TryGetValue(script_hash, out HashSet<uint> hashset))
                return false;
            if (!hashset.Remove(position))
                return false;
            if (hashset.Count == 0)
                break_points.Remove(script_hash);
            return true;
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
            {
                if (!Limits.CheckStackSize(this))
                {
                    State |= VMState.FAULT;
                    return;
                }

                context.EvaluationStack.Push(context.OpReader.SafeReadBytes((byte)opcode));
            }
            else
                switch (opcode)
                {
                    // Push value
                    case OpCode.PUSH0:
                        {
                            if (!Limits.CheckStackSize(this))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(new byte[0]);
                            break;
                        }
                    case OpCode.PUSHDATA1:
                        {
                            if (!Limits.CheckStackSize(this))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(context.OpReader.SafeReadBytes(context.OpReader.ReadByte()));
                            break;
                        }
                    case OpCode.PUSHDATA2:
                        {
                            if (!Limits.CheckStackSize(this))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(context.OpReader.SafeReadBytes(context.OpReader.ReadUInt16()));
                            break;
                        }
                    case OpCode.PUSHDATA4:
                        {
                            if (!Limits.CheckStackSize(this))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            int length = context.OpReader.ReadInt32();

                            if (!Limits.CheckMaxItemSize(length))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(context.OpReader.SafeReadBytes(length));
                            break;
                        }
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
                        {
                            if (!Limits.CheckStackSize(this))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push((int)opcode - (int)OpCode.PUSH1 + 1);
                            break;
                        }

                    // Control
                    case OpCode.NOP: break;
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
                                Limits.DecreaseStackItemWithoutStrict(this);
                                fValue = context.EvaluationStack.Pop().GetBoolean();

                                if (opcode == OpCode.JMPIFNOT)
                                    fValue = !fValue;
                            }
                            if (fValue)
                                context.InstructionPointer = offset;
                            break;
                        }
                    case OpCode.CALL:
                        {
                            if (!Limits.CheckMaxInvocationStack(this))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            ExecutionContext context_call = LoadScript(context.Script);
                            context.EvaluationStack.CopyTo(context_call.EvaluationStack);
                            context_call.InstructionPointer = context.InstructionPointer;
                            context.EvaluationStack.Clear();
                            context.InstructionPointer += 2;
                            ExecuteOp(OpCode.JMP, context_call);
                            break;
                        }
                    case OpCode.RET:
                        {
                            is_stackitem_count_strict = false;

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
                        }
                    case OpCode.APPCALL:
                    case OpCode.TAILCALL:
                        {
                            is_stackitem_count_strict = false;

                            if (table == null)
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            if (opcode == OpCode.APPCALL && !Limits.CheckMaxInvocationStack(this))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            byte[] script_hash = context.OpReader.SafeReadBytes(20);
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
                            break;
                        }
                    case OpCode.SYSCALL:
                        {
                            if (!Limits.CheckStackSize(this, false, int.MaxValue))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            if (Service?.Invoke(context.OpReader.ReadVarBytes(252), this) != true)
                                State |= VMState.FAULT;
                            break;
                        }

                    // Stack ops
                    case OpCode.DUPFROMALTSTACK:
                        {
                            if (!Limits.CheckStackSize(this))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(context.AltStack.Peek());
                            break;
                        }
                    case OpCode.TOALTSTACK:
                        {
                            context.AltStack.Push(context.EvaluationStack.Pop());
                            break;
                        }
                    case OpCode.FROMALTSTACK:
                        {
                            context.EvaluationStack.Push(context.AltStack.Pop());
                            break;
                        }
                    case OpCode.XDROP:
                        {
                            Limits.DecreaseStackItemWithoutStrict(this, 2);

                            int n = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (n < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            context.EvaluationStack.Remove(n);
                            break;
                        }
                    case OpCode.XSWAP:
                        {
                            Limits.DecreaseStackItem(this);

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
                            break;
                        }
                    case OpCode.XTUCK:
                        {
                            int n = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (n <= 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            context.EvaluationStack.Insert(n, context.EvaluationStack.Peek());
                            break;
                        }
                    case OpCode.DEPTH:
                        {
                            if (!Limits.CheckStackSize(this))
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            context.EvaluationStack.Push(context.EvaluationStack.Count);
                            break;
                        }
                    case OpCode.DROP:
                        {
                            Limits.DecreaseStackItemWithoutStrict(this);
                            context.EvaluationStack.Pop();
                            break;
                        }
                    case OpCode.DUP:
                        {
                            if (!Limits.CheckStackSize(this))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(context.EvaluationStack.Peek());
                            break;
                        }
                    case OpCode.NIP:
                        {
                            Limits.DecreaseStackItemWithoutStrict(this);
                            context.EvaluationStack.Remove(1);
                            break;
                        }
                    case OpCode.OVER:
                        {
                            if (!Limits.CheckStackSize(this))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(context.EvaluationStack.Peek(1));
                            break;
                        }
                    case OpCode.PICK:
                        {
                            int n = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (n < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            context.EvaluationStack.Push(context.EvaluationStack.Peek(n));
                            break;
                        }
                    case OpCode.ROLL:
                        {
                            Limits.DecreaseStackItem(this);

                            int n = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (n < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            if (n == 0) break;
                            context.EvaluationStack.Push(context.EvaluationStack.Remove(n));
                            break;
                        }
                    case OpCode.ROT:
                        {
                            context.EvaluationStack.Push(context.EvaluationStack.Remove(2));
                            break;
                        }
                    case OpCode.SWAP:
                        {
                            context.EvaluationStack.Push(context.EvaluationStack.Remove(1));
                            break;
                        }
                    case OpCode.TUCK:
                        {
                            if (!Limits.CheckStackSize(this))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Insert(2, context.EvaluationStack.Peek());
                            break;
                        }
                    case OpCode.CAT:
                        {
                            Limits.DecreaseStackItem(this);

                            byte[] x2 = context.EvaluationStack.Pop().GetByteArray();
                            byte[] x1 = context.EvaluationStack.Pop().GetByteArray();

                            if (!Limits.CheckMaxItemSize(x1.Length + x2.Length))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(x1.Concat(x2).ToArray());
                            break;
                        }
                    case OpCode.SUBSTR:
                        {
                            Limits.DecreaseStackItem(this, 2);

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
                            break;
                        }
                    case OpCode.LEFT:
                        {
                            Limits.DecreaseStackItem(this);

                            int count = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (count < 0)
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            byte[] x = context.EvaluationStack.Pop().GetByteArray();
                            context.EvaluationStack.Push(x.Take(count).ToArray());
                            break;
                        }
                    case OpCode.RIGHT:
                        {
                            Limits.DecreaseStackItem(this);

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
                            break;
                        }
                    case OpCode.SIZE:
                        {
                            byte[] x = context.EvaluationStack.Pop().GetByteArray();
                            context.EvaluationStack.Push(x.Length);
                            break;
                        }

                    // Bitwise logic
                    case OpCode.INVERT:
                        {
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(~x);
                            break;
                        }
                    case OpCode.AND:
                        {
                            Limits.DecreaseStackItem(this);

                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 & x2);
                            break;
                        }
                    case OpCode.OR:
                        {
                            Limits.DecreaseStackItem(this);

                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 | x2);
                            break;
                        }
                    case OpCode.XOR:
                        {
                            Limits.DecreaseStackItem(this);

                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 ^ x2);
                            break;
                        }
                    case OpCode.EQUAL:
                        {
                            Limits.DecreaseStackItemWithoutStrict(this);

                            StackItem x2 = context.EvaluationStack.Pop();
                            StackItem x1 = context.EvaluationStack.Pop();
                            context.EvaluationStack.Push(x1.Equals(x2));
                            break;
                        }

                    // Numeric
                    case OpCode.INC:
                        {
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();

                            if (!Limits.CheckBigInteger(x) || !Limits.CheckBigInteger(x + 1))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(x + 1);
                            break;
                        }
                    case OpCode.DEC:
                        {
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();

                            if (!Limits.CheckBigInteger(x) || (x.Sign <= 0 && !Limits.CheckBigInteger(x - 1)))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(x - 1);
                            break;
                        }
                    case OpCode.SIGN:
                        {
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x.Sign);
                            break;
                        }
                    case OpCode.NEGATE:
                        {
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(-x);
                            break;
                        }
                    case OpCode.ABS:
                        {
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(BigInteger.Abs(x));
                            break;
                        }
                    case OpCode.NOT:
                        {
                            is_stackitem_count_strict = false;

                            bool x = context.EvaluationStack.Pop().GetBoolean();
                            context.EvaluationStack.Push(!x);
                            break;
                        }
                    case OpCode.NZ:
                        {
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x != BigInteger.Zero);
                            break;
                        }
                    case OpCode.ADD:
                        {
                            Limits.DecreaseStackItem(this);

                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();

                            if (!Limits.CheckBigInteger(x2) || !Limits.CheckBigInteger(x1) || !Limits.CheckBigInteger(x1 + x2))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(x1 + x2);
                            break;
                        }
                    case OpCode.SUB:
                        {
                            Limits.DecreaseStackItem(this);

                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();

                            if (!Limits.CheckBigInteger(x2) || !Limits.CheckBigInteger(x1) || !Limits.CheckBigInteger(x1 - x2))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(x1 - x2);
                            break;
                        }
                    case OpCode.MUL:
                        {
                            Limits.DecreaseStackItem(this);

                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();

                            int lx1 = x1.ToByteArray().Length;

                            if (!Limits.CheckBigIntegerBitLength(lx1))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            int lx2 = x2.ToByteArray().Length;

                            if (!Limits.CheckBigIntegerBitLength(lx1 + lx2))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(x1 * x2);
                            break;
                        }
                    case OpCode.DIV:
                        {
                            Limits.DecreaseStackItem(this);

                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();

                            if (!Limits.CheckBigInteger(x2) || !Limits.CheckBigInteger(x1))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(x1 / x2);
                            break;
                        }
                    case OpCode.MOD:
                        {
                            Limits.DecreaseStackItem(this);

                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();

                            if (!Limits.CheckBigInteger(x2) || !Limits.CheckBigInteger(x1))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(x1 % x2);
                            break;
                        }
                    case OpCode.SHL:
                        {
                            Limits.DecreaseStackItem(this);

                            int shift = (int)context.EvaluationStack.Pop().GetBigInteger();

                            if (!Limits.CheckShift(shift))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();

                            if (!Limits.CheckBigInteger(x))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(x << shift);
                            break;
                        }
                    case OpCode.SHR:
                        {
                            Limits.DecreaseStackItem(this);

                            int shift = (int)context.EvaluationStack.Pop().GetBigInteger();

                            if (!Limits.CheckShift(shift))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();

                            if (!Limits.CheckBigInteger(x))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(x >> shift);
                            break;
                        }
                    case OpCode.BOOLAND:
                        {
                            Limits.DecreaseStackItemWithoutStrict(this);

                            bool x2 = context.EvaluationStack.Pop().GetBoolean();
                            bool x1 = context.EvaluationStack.Pop().GetBoolean();
                            context.EvaluationStack.Push(x1 && x2);
                            break;
                        }
                    case OpCode.BOOLOR:
                        {
                            Limits.DecreaseStackItemWithoutStrict(this);

                            bool x2 = context.EvaluationStack.Pop().GetBoolean();
                            bool x1 = context.EvaluationStack.Pop().GetBoolean();
                            context.EvaluationStack.Push(x1 || x2);
                            break;
                        }
                    case OpCode.NUMEQUAL:
                        {
                            Limits.DecreaseStackItem(this);

                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 == x2);
                            break;
                        }
                    case OpCode.NUMNOTEQUAL:
                        {
                            Limits.DecreaseStackItem(this);

                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 != x2);
                            break;
                        }
                    case OpCode.LT:
                        {
                            Limits.DecreaseStackItem(this);

                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 < x2);
                            break;
                        }
                    case OpCode.GT:
                        {
                            Limits.DecreaseStackItem(this);

                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 > x2);
                            break;
                        }
                    case OpCode.LTE:
                        {
                            Limits.DecreaseStackItem(this);

                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 <= x2);
                            break;
                        }
                    case OpCode.GTE:
                        {
                            Limits.DecreaseStackItem(this);

                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 >= x2);
                            break;
                        }
                    case OpCode.MIN:
                        {
                            Limits.DecreaseStackItem(this);

                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(BigInteger.Min(x1, x2));
                            break;
                        }
                    case OpCode.MAX:
                        {
                            Limits.DecreaseStackItem(this);

                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(BigInteger.Max(x1, x2));
                            break;
                        }
                    case OpCode.WITHIN:
                        {
                            Limits.DecreaseStackItem(this, 2);

                            BigInteger b = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger a = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(a <= x && x < b);
                            break;
                        }

                    // Crypto
                    case OpCode.SHA1:
                        using (SHA1 sha = SHA1.Create())
                        {
                            byte[] x = context.EvaluationStack.Pop().GetByteArray();
                            context.EvaluationStack.Push(sha.ComputeHash(x));
                            break;
                        }
                    case OpCode.SHA256:
                        using (SHA256 sha = SHA256.Create())
                        {
                            byte[] x = context.EvaluationStack.Pop().GetByteArray();
                            context.EvaluationStack.Push(sha.ComputeHash(x));
                            break;
                        }
                    case OpCode.HASH160:
                        {
                            byte[] x = context.EvaluationStack.Pop().GetByteArray();
                            context.EvaluationStack.Push(Crypto.Hash160(x));
                            break;
                        }
                    case OpCode.HASH256:
                        {
                            byte[] x = context.EvaluationStack.Pop().GetByteArray();
                            context.EvaluationStack.Push(Crypto.Hash256(x));
                            break;
                        }
                    case OpCode.CHECKSIG:
                        {
                            Limits.DecreaseStackItem(this);

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
                            break;
                        }
                    case OpCode.VERIFY:
                        {
                            Limits.DecreaseStackItem(this, 2);

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
                            break;
                        }
                    case OpCode.CHECKMULTISIG:
                        {
                            Limits.DecreaseStackItemWithoutStrict(this);

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
                            break;
                        }

                    // Array
                    case OpCode.ARRAYSIZE:
                        {
                            is_stackitem_count_strict = false;

                            StackItem item = context.EvaluationStack.Pop();
                            if (item is ICollection collection)
                                context.EvaluationStack.Push(collection.Count);
                            else
                                context.EvaluationStack.Push(item.GetByteArray().Length);
                            break;
                        }
                    case OpCode.PACK:
                        {
                            int size = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (size < 0 || size > context.EvaluationStack.Count || !Limits.CheckArraySize(size))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            List<StackItem> items = new List<StackItem>(size);
                            for (int i = 0; i < size; i++)
                                items.Add(context.EvaluationStack.Pop());
                            context.EvaluationStack.Push(items);
                            break;
                        }
                    case OpCode.UNPACK:
                        {
                            StackItem item = context.EvaluationStack.Pop();
                            if (item is VMArray array)
                            {
                                if (!Limits.CheckStackSize(this, false, array.Count))
                                {
                                    State |= VMState.FAULT;
                                    return;
                                }

                                for (int i = array.Count - 1; i >= 0; i--)
                                    context.EvaluationStack.Push(array[i]);
                                context.EvaluationStack.Push(array.Count);
                            }
                            else
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            break;
                        }
                    case OpCode.PICKITEM:
                        {
                            if (!Limits.CheckStackSize(this, false, int.MaxValue))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

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
                            if (!Limits.CheckStackSize(this, false, int.MaxValue))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

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
                                    {
                                        int index = (int)key.GetBigInteger();
                                        if (index < 0 || index >= array.Count)
                                        {
                                            State |= VMState.FAULT;
                                            return;
                                        }
                                        array[index] = value;
                                        break;
                                    }
                                case Map map:
                                    {
                                        if (!Limits.CheckArraySize(map.Count + 1))
                                        {
                                            State |= VMState.FAULT;
                                        }

                                        map[key] = value;
                                        break;
                                    }
                                default:
                                    {
                                        State |= VMState.FAULT;
                                        return;
                                    }
                            }
                        }
                        break;
                    case OpCode.NEWARRAY:
                        {
                            int count = (int)context.EvaluationStack.Pop().GetBigInteger();

                            if (!Limits.CheckArraySize(count) || !Limits.CheckStackSize(this, false, count))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

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

                            if (!Limits.CheckArraySize(count) || !Limits.CheckStackSize(this, false, count))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            List<StackItem> items = new List<StackItem>(count);
                            for (var i = 0; i < count; i++)
                            {
                                items.Add(false);
                            }
                            context.EvaluationStack.Push(new VM.Types.Struct(items));
                            break;
                        }
                    case OpCode.NEWMAP:
                        {
                            if (!Limits.CheckStackSize(this))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(new Map());
                            break;
                        }
                    case OpCode.APPEND:
                        {
                            if (!Limits.CheckStackSize(this, false, int.MaxValue))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

                            StackItem newItem = context.EvaluationStack.Pop();
                            if (newItem is Types.Struct s)
                            {
                                newItem = s.Clone();
                            }
                            StackItem arrItem = context.EvaluationStack.Pop();
                            if (arrItem is VMArray array)
                            {
                                if (!Limits.CheckArraySize(array.Count + 1))
                                {
                                    State |= VMState.FAULT;
                                }

                                array.Add(newItem);
                            }
                            else
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            break;
                        }
                    case OpCode.REVERSE:
                        {
                            Limits.DecreaseStackItemWithoutStrict(this);

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
                            break;
                        }
                    case OpCode.REMOVE:
                        {
                            Limits.DecreaseStackItemWithoutStrict(this, 2);

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
                            break;
                        }
                    case OpCode.HASKEY:
                        {
                            Limits.DecreaseStackItemWithoutStrict(this);

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
                            break;
                        }
                    case OpCode.KEYS:
                        {
                            switch (context.EvaluationStack.Pop())
                            {
                                case Map map:
                                    {
                                        if (!Limits.CheckStackSize(this, false, map.Count))
                                        {
                                            State |= VMState.FAULT;
                                            return;
                                        }

                                        context.EvaluationStack.Push(new VMArray(map.Keys));
                                        break;
                                    }
                                default:
                                    {
                                        State |= VMState.FAULT;
                                        return;
                                    }
                            }
                            break;
                        }
                    case OpCode.VALUES:
                        {
                            if (!Limits.CheckStackSize(this, false, int.MaxValue))
                            {
                                State |= VMState.FAULT;
                                return;
                            }

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
                            if (!Limits.CheckMaxInvocationStack(this))
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
                            ExecutionContext context_call = LoadScript(context.Script, rvcount);
                            context.EvaluationStack.CopyTo(context_call.EvaluationStack, pcount);
                            context_call.InstructionPointer = context.InstructionPointer;
                            for (int i = 0; i < pcount; i++)
                                context.EvaluationStack.Pop();
                            context.InstructionPointer += 2;
                            ExecuteOp(OpCode.JMP, context_call);
                            break;
                        }
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
                            else
                            {
                                if (!Limits.CheckMaxInvocationStack(this))
                                {
                                    State |= VMState.FAULT;
                                    return;
                                }
                            }

                            byte[] script_hash;
                            if (opcode == OpCode.CALL_ED || opcode == OpCode.CALL_EDT)
                            {
                                Limits.DecreaseStackItem(this);

                                script_hash = context.EvaluationStack.Pop().GetByteArray();
                            }
                            else
                            {
                                script_hash = context.OpReader.SafeReadBytes(20);
                            }

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
                            break;
                        }

                    // Exceptions
                    case OpCode.THROW:
                        {
                            State |= VMState.FAULT;
                            return;
                        }
                    case OpCode.THROWIFNOT:
                        {
                            Limits.DecreaseStackItemWithoutStrict(this);

                            if (!context.EvaluationStack.Pop().GetBoolean())
                            {
                                State |= VMState.FAULT;
                                return;
                            }
                            break;
                        }
                    default:
                        {
                            State |= VMState.FAULT;
                            return;
                        }
                }
            if (!State.HasFlag(VMState.FAULT) && InvocationStack.Count > 0)
            {
                if (break_points != null && break_points.TryGetValue(CurrentContext.ScriptHash, out HashSet<uint> hashset) && hashset.Contains((uint)CurrentContext.InstructionPointer))
                    State |= VMState.BREAK;
            }
        }

        public ExecutionContext LoadScript(byte[] script, int rvcount = -1)
        {
            ExecutionContext context = new ExecutionContext(this, script, rvcount);
            InvocationStack.Push(context);
            return context;
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
