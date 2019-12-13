using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using VMArray = Neo.VM.Types.Array;

namespace Neo.VM
{
    public class ExecutionEngine : IDisposable
    {
        #region Limits Variables

        /// <summary>
        /// Max value for SHL and SHR
        /// </summary>
        public virtual int MaxShift => 256;

        /// <summary>
        /// Set the max Stack Size
        /// </summary>
        public virtual uint MaxStackSize => 2 * 1024;

        /// <summary>
        /// Set Max Item Size
        /// </summary>
        public virtual uint MaxItemSize => 1024 * 1024;

        /// <summary>
        /// Set Max Invocation Stack Size
        /// </summary>
        public virtual uint MaxInvocationStackSize => 1024;

        #endregion

        public ReferenceCounter ReferenceCounter { get; } = new ReferenceCounter();
        public Stack<ExecutionContext> InvocationStack { get; } = new Stack<ExecutionContext>();
        public ExecutionContext CurrentContext { get; private set; }
        public ExecutionContext EntryContext { get; private set; }
        public EvaluationStack ResultStack { get; }
        public VMState State { get; internal protected set; } = VMState.BREAK;

        public ExecutionEngine()
        {
            ResultStack = new EvaluationStack(ReferenceCounter);
        }

        #region Limits

        /// <summary>
        /// Check if the is possible to overflow the MaxItemSize
        /// </summary>
        /// <param name="length">Length</param>
        /// <returns>Return True if are allowed, otherwise False</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckMaxItemSize(int length) => length >= 0 && length <= MaxItemSize;

        /// <summary>
        /// Check if the number is allowed from SHL and SHR
        /// </summary>
        /// <param name="shift">Shift</param>
        /// <returns>Return True if are allowed, otherwise False</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckShift(int shift) => shift <= MaxShift && shift >= 0;

        #endregion

        protected virtual void ContextUnloaded(ExecutionContext context)
        {
        }

        public virtual void Dispose()
        {
            InvocationStack.Clear();
        }

        public virtual VMState Execute()
        {
            if (State == VMState.BREAK)
                State = VMState.NONE;
            while (State != VMState.HALT && State != VMState.FAULT)
                ExecuteNext();
            return State;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ExecuteCall(int position)
        {
            if (position < 0 || position > CurrentContext.Script.Length) return false;
            ExecutionContext context_call = CurrentContext.Clone();
            context_call.InstructionPointer = position;
            LoadContext(context_call);
            return true;
        }

        private bool ExecuteInstruction()
        {
            ExecutionContext context = CurrentContext;
            Instruction instruction = context.CurrentInstruction;
            switch (instruction.OpCode)
            {
                //Push
                case OpCode.PUSHINT8:
                case OpCode.PUSHINT16:
                case OpCode.PUSHINT32:
                case OpCode.PUSHINT64:
                case OpCode.PUSHINT128:
                case OpCode.PUSHINT256:
                    {
                        Push(new BigInteger(instruction.Operand.Span));
                        break;
                    }
                case OpCode.PUSHA:
                    {
                        int position = instruction.TokenI32;
                        if (position < 0 || position > CurrentContext.Script.Length) return false;
                        Push(new Pointer(position));
                        break;
                    }
                case OpCode.PUSHNULL:
                    {
                        Push(StackItem.Null);
                        break;
                    }
                case OpCode.PUSHDATA1:
                case OpCode.PUSHDATA2:
                case OpCode.PUSHDATA4:
                    {
                        if (!CheckMaxItemSize(instruction.Operand.Length)) return false;
                        Push(instruction.Operand);
                        break;
                    }
                case OpCode.PUSHM1:
                case OpCode.PUSH0:
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
                        Push((int)instruction.OpCode - (int)OpCode.PUSH0);
                        break;
                    }

                // Control
                case OpCode.NOP: break;
                case OpCode.JMP:
                    {
                        return ExecuteJump(true, instruction.TokenI8);
                    }
                case OpCode.JMP_L:
                    {
                        return ExecuteJump(true, instruction.TokenI32);
                    }
                case OpCode.JMPIF:
                    {
                        if (!TryPop(out StackItem x)) return false;
                        return ExecuteJump(x.ToBoolean(), instruction.TokenI8);
                    }
                case OpCode.JMPIF_L:
                    {
                        if (!TryPop(out StackItem x)) return false;
                        return ExecuteJump(x.ToBoolean(), instruction.TokenI32);
                    }
                case OpCode.JMPIFNOT:
                    {
                        if (!TryPop(out StackItem x)) return false;
                        return ExecuteJump(!x.ToBoolean(), instruction.TokenI8);
                    }
                case OpCode.JMPIFNOT_L:
                    {
                        if (!TryPop(out StackItem x)) return false;
                        return ExecuteJump(!x.ToBoolean(), instruction.TokenI32);
                    }
                case OpCode.JMPEQ:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        return ExecuteJump(x1.ToBigInteger() == x2.ToBigInteger(), instruction.TokenI8);
                    }
                case OpCode.JMPEQ_L:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        return ExecuteJump(x1.ToBigInteger() == x2.ToBigInteger(), instruction.TokenI32);
                    }
                case OpCode.JMPNE:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        return ExecuteJump(x1.ToBigInteger() != x2.ToBigInteger(), instruction.TokenI8);
                    }
                case OpCode.JMPNE_L:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        return ExecuteJump(x1.ToBigInteger() != x2.ToBigInteger(), instruction.TokenI32);
                    }
                case OpCode.JMPGT:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        return ExecuteJump(x1.ToBigInteger() > x2.ToBigInteger(), instruction.TokenI8);
                    }
                case OpCode.JMPGT_L:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        return ExecuteJump(x1.ToBigInteger() > x2.ToBigInteger(), instruction.TokenI32);
                    }
                case OpCode.JMPGE:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        return ExecuteJump(x1.ToBigInteger() >= x2.ToBigInteger(), instruction.TokenI8);
                    }
                case OpCode.JMPGE_L:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        return ExecuteJump(x1.ToBigInteger() >= x2.ToBigInteger(), instruction.TokenI32);
                    }
                case OpCode.JMPLT:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        return ExecuteJump(x1.ToBigInteger() < x2.ToBigInteger(), instruction.TokenI8);
                    }
                case OpCode.JMPLT_L:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        return ExecuteJump(x1.ToBigInteger() < x2.ToBigInteger(), instruction.TokenI32);
                    }
                case OpCode.JMPLE:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        return ExecuteJump(x1.ToBigInteger() <= x2.ToBigInteger(), instruction.TokenI8);
                    }
                case OpCode.JMPLE_L:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        return ExecuteJump(x1.ToBigInteger() <= x2.ToBigInteger(), instruction.TokenI32);
                    }
                case OpCode.CALL:
                    {
                        if (!ExecuteCall(checked(context.InstructionPointer + instruction.TokenI8)))
                            return false;
                        break;
                    }
                case OpCode.CALL_L:
                    {
                        if (!ExecuteCall(checked(context.InstructionPointer + instruction.TokenI32)))
                            return false;
                        break;
                    }
                case OpCode.CALLA:
                    {
                        if (!TryPop(out Pointer x)) return false;
                        if (!ExecuteCall(x.Position)) return false;
                        break;
                    }
                case OpCode.THROW:
                    {
                        return false;
                    }
                case OpCode.THROWIF:
                case OpCode.THROWIFNOT:
                    {
                        if (!TryPop(out StackItem x)) return false;
                        if (x.ToBoolean() ^ (instruction.OpCode == OpCode.THROWIFNOT)) return false;
                        break;
                    }
                case OpCode.RET:
                    {
                        ExecutionContext context_pop = InvocationStack.Pop();
                        int rvcount = context_pop.RVCount;
                        if (rvcount == -1) rvcount = context_pop.EvaluationStack.Count;
                        EvaluationStack stack_eval;
                        if (InvocationStack.Count == 0)
                        {
                            EntryContext = null;
                            CurrentContext = null;
                            stack_eval = ResultStack;
                        }
                        else
                        {
                            CurrentContext = InvocationStack.Peek();
                            stack_eval = CurrentContext.EvaluationStack;
                        }
                        if (context_pop.EvaluationStack == stack_eval)
                        {
                            if (context_pop.RVCount != 0) return false;
                        }
                        else
                        {
                            if (context_pop.EvaluationStack.Count != rvcount) return false;
                            if (rvcount > 0)
                                context_pop.EvaluationStack.CopyTo(stack_eval);
                        }
                        if (InvocationStack.Count == 0 || context_pop.AltStack != CurrentContext.AltStack)
                        {
                            context_pop.AltStack.Clear();
                        }
                        if (InvocationStack.Count == 0)
                        {
                            State = VMState.HALT;
                        }
                        ContextUnloaded(context_pop);
                        return true;
                    }
                case OpCode.SYSCALL:
                    {
                        if (!OnSysCall(instruction.TokenU32))
                            return false;
                        break;
                    }

                // Stack ops
                case OpCode.DEPTH:
                    {
                        Push(context.EvaluationStack.Count);
                        break;
                    }
                case OpCode.DROP:
                    {
                        if (!TryPop(out StackItem _)) return false;
                        break;
                    }
                case OpCode.NIP:
                    {
                        if (!context.EvaluationStack.TryRemove(1, out StackItem _)) return false;
                        break;
                    }
                case OpCode.XDROP:
                    {
                        if (!TryPop(out PrimitiveType item_n)) return false;
                        int n = (int)item_n.ToBigInteger();
                        if (n < 0) return false;
                        if (!context.EvaluationStack.TryRemove(n, out StackItem _)) return false;
                        break;
                    }
                case OpCode.CLEAR:
                    {
                        context.EvaluationStack.Clear();
                        break;
                    }
                case OpCode.DUP:
                    {
                        Push(Peek());
                        break;
                    }
                case OpCode.OVER:
                    {
                        Push(Peek(1));
                        break;
                    }
                case OpCode.PICK:
                    {
                        if (!TryPop(out PrimitiveType item_n)) return false;
                        int n = (int)item_n.ToBigInteger();
                        if (n < 0) return false;
                        Push(Peek(n));
                        break;
                    }
                case OpCode.TUCK:
                    {
                        context.EvaluationStack.Insert(2, Peek());
                        break;
                    }
                case OpCode.SWAP:
                    {
                        if (!context.EvaluationStack.TryRemove(1, out StackItem x)) return false;
                        Push(x);
                        break;
                    }
                case OpCode.ROT:
                    {
                        if (!context.EvaluationStack.TryRemove(2, out StackItem x)) return false;
                        Push(x);
                        break;
                    }
                case OpCode.ROLL:
                    {
                        if (!TryPop(out PrimitiveType item_n)) return false;
                        int n = (int)item_n.ToBigInteger();
                        if (n < 0) return false;
                        if (n == 0) break;
                        if (!context.EvaluationStack.TryRemove(n, out StackItem x)) return false;
                        Push(x);
                        break;
                    }
                case OpCode.REVERSE3:
                    {
                        if (!context.EvaluationStack.Reverse(3)) return false;
                        break;
                    }
                case OpCode.REVERSE4:
                    {
                        if (!context.EvaluationStack.Reverse(4)) return false;
                        break;
                    }
                case OpCode.REVERSEN:
                    {
                        if (!TryPop(out PrimitiveType n)) return false;
                        if (!context.EvaluationStack.Reverse((int)n.ToBigInteger())) return false;
                        break;
                    }
                case OpCode.DUPFROMALTSTACKBOTTOM:
                    {
                        Push(context.AltStack.Peek(-1));
                        break;
                    }
                case OpCode.DUPFROMALTSTACK:
                    {
                        Push(context.AltStack.Peek());
                        break;
                    }
                case OpCode.TOALTSTACK:
                    {
                        if (!TryPop(out StackItem x)) return false;
                        context.AltStack.Push(x);
                        break;
                    }
                case OpCode.FROMALTSTACK:
                    {
                        if (!context.AltStack.TryPop(out StackItem x)) return false;
                        Push(x);
                        break;
                    }
                case OpCode.ISNULL:
                    {
                        if (!TryPop(out StackItem x)) return false;
                        Push(x.IsNull);
                        break;
                    }

                // Splice
                case OpCode.CAT:
                    {
                        if (!TryPop(out PrimitiveType item_x2)) return false;
                        if (!TryPop(out PrimitiveType item_x1)) return false;
                        ReadOnlyMemory<byte> x2 = item_x2.ToMemory();
                        ReadOnlyMemory<byte> x1 = item_x1.ToMemory();
                        StackItem result;
                        if (x1.IsEmpty)
                        {
                            result = x2;
                        }
                        else if (x2.IsEmpty)
                        {
                            result = x1;
                        }
                        else
                        {
                            int length = x1.Length + x2.Length;
                            if (!CheckMaxItemSize(length)) return false;
                            byte[] dstBuffer = new byte[length];
                            x1.CopyTo(dstBuffer);
                            x2.CopyTo(dstBuffer.AsMemory(x1.Length));
                            result = dstBuffer;
                        }
                        Push(result);
                        break;
                    }
                case OpCode.SUBSTR:
                    {
                        if (!TryPop(out PrimitiveType item_count)) return false;
                        int count = (int)item_count.ToBigInteger();
                        if (count < 0) return false;
                        if (count > MaxItemSize) count = (int)MaxItemSize;
                        if (!TryPop(out PrimitiveType item_index)) return false;
                        int index = (int)item_index.ToBigInteger();
                        if (index < 0) return false;
                        if (!TryPop(out PrimitiveType item_x)) return false;
                        ReadOnlyMemory<byte> x = item_x.ToMemory();
                        if (index > x.Length) return false;
                        if (index + count > x.Length) count = x.Length - index;
                        Push(x.Slice(index, count));
                        break;
                    }
                case OpCode.LEFT:
                    {
                        if (!TryPop(out PrimitiveType item_count)) return false;
                        int count = (int)item_count.ToBigInteger();
                        if (count < 0) return false;
                        if (!TryPop(out PrimitiveType item_x)) return false;
                        ReadOnlyMemory<byte> x = item_x.ToMemory();
                        if (count < x.Length) x = x[0..count];
                        Push(x);
                        break;
                    }
                case OpCode.RIGHT:
                    {
                        if (!TryPop(out PrimitiveType item_count)) return false;
                        int count = (int)item_count.ToBigInteger();
                        if (count < 0) return false;
                        if (!TryPop(out PrimitiveType item_x)) return false;
                        ReadOnlyMemory<byte> x = item_x.ToMemory();
                        if (count > x.Length) return false;
                        if (count < x.Length) x = x[^count..^0];
                        Push(x);
                        break;
                    }
                case OpCode.SIZE:
                    {
                        if (!TryPop(out PrimitiveType x)) return false;
                        Push(x.GetByteLength());
                        break;
                    }

                // Bitwise logic
                case OpCode.INVERT:
                    {
                        if (!TryPop(out PrimitiveType x)) return false;
                        Push(~x.ToBigInteger());
                        break;
                    }
                case OpCode.AND:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        Push(x1.ToBigInteger() & x2.ToBigInteger());
                        break;
                    }
                case OpCode.OR:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        Push(x1.ToBigInteger() | x2.ToBigInteger());
                        break;
                    }
                case OpCode.XOR:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        Push(x1.ToBigInteger() ^ x2.ToBigInteger());
                        break;
                    }
                case OpCode.EQUAL:
                    {
                        if (!TryPop(out StackItem x2)) return false;
                        if (!TryPop(out StackItem x1)) return false;
                        Push(x1.Equals(x2));
                        break;
                    }

                // Numeric
                case OpCode.INC:
                    {
                        if (!TryPop(out PrimitiveType x)) return false;
                        Push(x.ToBigInteger() + 1);
                        break;
                    }
                case OpCode.DEC:
                    {
                        if (!TryPop(out PrimitiveType x)) return false;
                        Push(x.ToBigInteger() - 1);
                        break;
                    }
                case OpCode.SIGN:
                    {
                        if (!TryPop(out PrimitiveType x)) return false;
                        Push(x.ToBigInteger().Sign);
                        break;
                    }
                case OpCode.NEGATE:
                    {
                        if (!TryPop(out PrimitiveType x)) return false;
                        Push(-x.ToBigInteger());
                        break;
                    }
                case OpCode.ABS:
                    {
                        if (!TryPop(out PrimitiveType x)) return false;
                        Push(BigInteger.Abs(x.ToBigInteger()));
                        break;
                    }
                case OpCode.NOT:
                    {
                        if (!TryPop(out StackItem x)) return false;
                        Push(!x.ToBoolean());
                        break;
                    }
                case OpCode.NZ:
                    {
                        if (!TryPop(out PrimitiveType x)) return false;
                        Push(!x.ToBigInteger().IsZero);
                        break;
                    }
                case OpCode.ADD:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        Push(x1.ToBigInteger() + x2.ToBigInteger());
                        break;
                    }
                case OpCode.SUB:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        Push(x1.ToBigInteger() - x2.ToBigInteger());
                        break;
                    }
                case OpCode.MUL:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        Push(x1.ToBigInteger() * x2.ToBigInteger());
                        break;
                    }
                case OpCode.DIV:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        Push(x1.ToBigInteger() / x2.ToBigInteger());
                        break;
                    }
                case OpCode.MOD:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        Push(x1.ToBigInteger() % x2.ToBigInteger());
                        break;
                    }
                case OpCode.SHL:
                    {
                        if (!TryPop(out PrimitiveType item_shift)) return false;
                        int shift = (int)item_shift.ToBigInteger();
                        if (!CheckShift(shift)) return false;
                        if (shift == 0) break;
                        if (!TryPop(out PrimitiveType x)) return false;
                        Push(x.ToBigInteger() << shift);
                        break;
                    }
                case OpCode.SHR:
                    {
                        if (!TryPop(out PrimitiveType item_shift)) return false;
                        int shift = (int)item_shift.ToBigInteger();
                        if (!CheckShift(shift)) return false;
                        if (shift == 0) break;
                        if (!TryPop(out PrimitiveType x)) return false;
                        Push(x.ToBigInteger() >> shift);
                        break;
                    }
                case OpCode.BOOLAND:
                    {
                        if (!TryPop(out StackItem x2)) return false;
                        if (!TryPop(out StackItem x1)) return false;
                        Push(x1.ToBoolean() && x2.ToBoolean());
                        break;
                    }
                case OpCode.BOOLOR:
                    {
                        if (!TryPop(out StackItem x2)) return false;
                        if (!TryPop(out StackItem x1)) return false;
                        Push(x1.ToBoolean() || x2.ToBoolean());
                        break;
                    }
                case OpCode.NUMEQUAL:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        Push(x1.ToBigInteger() == x2.ToBigInteger());
                        break;
                    }
                case OpCode.NUMNOTEQUAL:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        Push(x1.ToBigInteger() != x2.ToBigInteger());
                        break;
                    }
                case OpCode.LT:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        Push(x1.ToBigInteger() < x2.ToBigInteger());
                        break;
                    }
                case OpCode.GT:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        Push(x1.ToBigInteger() > x2.ToBigInteger());
                        break;
                    }
                case OpCode.LTE:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        Push(x1.ToBigInteger() <= x2.ToBigInteger());
                        break;
                    }
                case OpCode.GTE:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        Push(x1.ToBigInteger() >= x2.ToBigInteger());
                        break;
                    }
                case OpCode.MIN:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        Push(BigInteger.Min(x1.ToBigInteger(), x2.ToBigInteger()));
                        break;
                    }
                case OpCode.MAX:
                    {
                        if (!TryPop(out PrimitiveType x2)) return false;
                        if (!TryPop(out PrimitiveType x1)) return false;
                        Push(BigInteger.Max(x1.ToBigInteger(), x2.ToBigInteger()));
                        break;
                    }
                case OpCode.WITHIN:
                    {
                        if (!TryPop(out PrimitiveType item_b)) return false;
                        if (!TryPop(out PrimitiveType item_a)) return false;
                        if (!TryPop(out PrimitiveType item_x)) return false;
                        BigInteger b = item_b.ToBigInteger();
                        BigInteger a = item_a.ToBigInteger();
                        BigInteger x = item_x.ToBigInteger();
                        Push(a <= x && x < b);
                        break;
                    }

                // Array
                case OpCode.ARRAYSIZE:
                    {
                        if (!TryPop(out StackItem x)) return false;
                        switch (x)
                        {
                            case CompoundType compound:
                                Push(compound.Count);
                                break;
                            case PrimitiveType primitive:
                                Push(primitive.GetByteLength());
                                break;
                            default:
                                return false;
                        }
                        break;
                    }
                case OpCode.PACK:
                    {
                        if (!TryPop(out PrimitiveType item_size)) return false;
                        int size = (int)item_size.ToBigInteger();
                        if (size < 0 || size > context.EvaluationStack.Count)
                            return false;
                        VMArray array = new VMArray(ReferenceCounter);
                        for (int i = 0; i < size; i++)
                        {
                            if (!TryPop(out StackItem item)) return false;
                            array.Add(item);
                        }
                        Push(array);
                        break;
                    }
                case OpCode.UNPACK:
                    {
                        if (!TryPop(out VMArray array)) return false;
                        for (int i = array.Count - 1; i >= 0; i--)
                            Push(array[i]);
                        Push(array.Count);
                        break;
                    }
                case OpCode.PICKITEM:
                    {
                        if (!TryPop(out PrimitiveType key)) return false;
                        if (!TryPop(out StackItem x)) return false;
                        switch (x)
                        {
                            case VMArray array:
                                {
                                    int index = (int)key.ToBigInteger();
                                    if (index < 0 || index >= array.Count) return false;
                                    Push(array[index]);
                                    break;
                                }
                            case Map map:
                                {
                                    if (!map.TryGetValue(key, out StackItem value)) return false;
                                    Push(value);
                                    break;
                                }
                            case PrimitiveType primitive:
                                {
                                    ReadOnlySpan<byte> byteArray = primitive.ToByteArray();
                                    int index = (int)key.ToBigInteger();
                                    if (index < 0 || index >= byteArray.Length) return false;
                                    Push((BigInteger)byteArray[index]);
                                    break;
                                }
                            default:
                                return false;
                        }
                        break;
                    }
                case OpCode.SETITEM:
                    {
                        if (!TryPop(out StackItem value)) return false;
                        if (value is Struct s) value = s.Clone();
                        if (!TryPop(out PrimitiveType key)) return false;
                        if (!TryPop(out StackItem x)) return false;
                        switch (x)
                        {
                            case VMArray array:
                                {
                                    int index = (int)key.ToBigInteger();
                                    array[index] = value;
                                    break;
                                }
                            case Map map:
                                {
                                    map[key] = value;
                                    break;
                                }
                            default:
                                return false;
                        }
                        break;
                    }
                case OpCode.NEWARRAY:
                case OpCode.NEWSTRUCT:
                    {
                        if (!TryPop(out StackItem x)) return false;
                        switch (x)
                        {
                            // Allow to convert between array and struct
                            case VMArray array:
                                {
                                    VMArray result;
                                    if (array is Struct)
                                    {
                                        if (instruction.OpCode == OpCode.NEWSTRUCT)
                                            result = array;
                                        else
                                            result = new VMArray(ReferenceCounter, array);
                                    }
                                    else
                                    {
                                        if (instruction.OpCode == OpCode.NEWARRAY)
                                            result = array;
                                        else
                                            result = new Struct(ReferenceCounter, array);
                                    }
                                    Push(result);
                                }
                                break;
                            case PrimitiveType primitive:
                                {
                                    int count = (int)primitive.ToBigInteger();
                                    if (count < 0 || count > MaxStackSize) return false;
                                    VMArray result = instruction.OpCode == OpCode.NEWARRAY
                                        ? new VMArray(ReferenceCounter)
                                        : new Struct(ReferenceCounter);
                                    for (var i = 0; i < count; i++)
                                        result.Add(StackItem.Null);
                                    Push(result);
                                }
                                break;
                            default:
                                return false;
                        }
                        break;
                    }
                case OpCode.NEWMAP:
                    {
                        Push(new Map(ReferenceCounter));
                        break;
                    }
                case OpCode.APPEND:
                    {
                        if (!TryPop(out StackItem newItem)) return false;
                        if (!TryPop(out VMArray array)) return false;
                        if (newItem is Struct s) newItem = s.Clone();
                        array.Add(newItem);
                        break;
                    }
                case OpCode.REVERSE:
                    {
                        if (!TryPop(out VMArray array)) return false;
                        array.Reverse();
                        break;
                    }
                case OpCode.REMOVE:
                    {
                        if (!TryPop(out PrimitiveType key)) return false;
                        if (!TryPop(out StackItem x)) return false;
                        switch (x)
                        {
                            case VMArray array:
                                int index = (int)key.ToBigInteger();
                                array.RemoveAt(index);
                                break;
                            case Map map:
                                map.Remove(key);
                                break;
                            default:
                                return false;
                        }
                        break;
                    }
                case OpCode.HASKEY:
                    {
                        if (!TryPop(out PrimitiveType key)) return false;
                        if (!TryPop(out StackItem x)) return false;
                        switch (x)
                        {
                            case VMArray array:
                                int index = (int)key.ToBigInteger();
                                if (index < 0) return false;
                                Push(index < array.Count);
                                break;
                            case Map map:
                                Push(map.ContainsKey(key));
                                break;
                            default:
                                return false;
                        }
                        break;
                    }
                case OpCode.KEYS:
                    {
                        if (!TryPop(out Map map)) return false;
                        Push(new VMArray(ReferenceCounter, map.Keys));
                        break;
                    }
                case OpCode.VALUES:
                    {
                        IEnumerable<StackItem> values;
                        if (!TryPop(out StackItem x)) return false;
                        switch (x)
                        {
                            case VMArray array:
                                values = array;
                                break;
                            case Map map:
                                values = map.Values;
                                break;
                            default:
                                return false;
                        }
                        VMArray newArray = new VMArray(ReferenceCounter);
                        foreach (StackItem item in values)
                            if (item is Struct s)
                                newArray.Add(s.Clone());
                            else
                                newArray.Add(item);
                        Push(newArray);
                        break;
                    }

                default:
                    return false;
            }
            context.MoveNext();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ExecuteJump(bool condition, int offset)
        {
            offset = checked(CurrentContext.InstructionPointer + offset);
            if (offset < 0 || offset > CurrentContext.Script.Length) return false;
            if (condition)
                CurrentContext.InstructionPointer = offset;
            else
                CurrentContext.MoveNext();
            return true;
        }

        internal protected void ExecuteNext()
        {
            if (InvocationStack.Count == 0)
            {
                State = VMState.HALT;
            }
            else
            {
                try
                {
                    Instruction instruction = CurrentContext.CurrentInstruction;
                    if (!PreExecuteInstruction() || !ExecuteInstruction() || !PostExecuteInstruction(instruction))
                        State = VMState.FAULT;
                }
                catch
                {
                    State = VMState.FAULT;
                }
            }
        }

        protected virtual void LoadContext(ExecutionContext context)
        {
            if (InvocationStack.Count >= MaxInvocationStackSize)
                throw new InvalidOperationException();
            InvocationStack.Push(context);
            if (EntryContext is null) EntryContext = context;
            CurrentContext = context;
        }

        public ExecutionContext LoadScript(Script script, int rvcount = -1)
        {
            ExecutionContext context = new ExecutionContext(script, rvcount, ReferenceCounter);
            LoadContext(context);
            return context;
        }

        protected virtual bool OnSysCall(uint method) => false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackItem Peek(int index = 0)
        {
            return CurrentContext.EvaluationStack.Peek(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackItem Pop()
        {
            return CurrentContext.EvaluationStack.Pop();
        }

        protected virtual bool PostExecuteInstruction(Instruction instruction)
        {
            return ReferenceCounter.CheckZeroReferred() <= MaxStackSize;
        }

        protected virtual bool PreExecuteInstruction() => true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(StackItem item)
        {
            CurrentContext.EvaluationStack.Push(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop<T>(out T item) where T : StackItem
        {
            return CurrentContext.EvaluationStack.TryPop(out item);
        }
    }
}
