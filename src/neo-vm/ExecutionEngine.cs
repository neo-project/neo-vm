using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Array = System.Array;
using Buffer = Neo.VM.Types.Buffer;
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

        public VMState Execute()
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
                        Push(new Pointer(context.Script, position));
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
                        if (!TryPop(out bool x)) return false;
                        return ExecuteJump(x, instruction.TokenI8);
                    }
                case OpCode.JMPIF_L:
                    {
                        if (!TryPop(out bool x)) return false;
                        return ExecuteJump(x, instruction.TokenI32);
                    }
                case OpCode.JMPIFNOT:
                    {
                        if (!TryPop(out bool x)) return false;
                        return ExecuteJump(!x, instruction.TokenI8);
                    }
                case OpCode.JMPIFNOT_L:
                    {
                        if (!TryPop(out bool x)) return false;
                        return ExecuteJump(!x, instruction.TokenI32);
                    }
                case OpCode.JMPEQ:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        return ExecuteJump(x1 == x2, instruction.TokenI8);
                    }
                case OpCode.JMPEQ_L:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        return ExecuteJump(x1 == x2, instruction.TokenI32);
                    }
                case OpCode.JMPNE:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        return ExecuteJump(x1 != x2, instruction.TokenI8);
                    }
                case OpCode.JMPNE_L:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        return ExecuteJump(x1 != x2, instruction.TokenI32);
                    }
                case OpCode.JMPGT:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        return ExecuteJump(x1 > x2, instruction.TokenI8);
                    }
                case OpCode.JMPGT_L:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        return ExecuteJump(x1 > x2, instruction.TokenI32);
                    }
                case OpCode.JMPGE:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        return ExecuteJump(x1 >= x2, instruction.TokenI8);
                    }
                case OpCode.JMPGE_L:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        return ExecuteJump(x1 >= x2, instruction.TokenI32);
                    }
                case OpCode.JMPLT:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        return ExecuteJump(x1 < x2, instruction.TokenI8);
                    }
                case OpCode.JMPLT_L:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        return ExecuteJump(x1 < x2, instruction.TokenI32);
                    }
                case OpCode.JMPLE:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        return ExecuteJump(x1 <= x2, instruction.TokenI8);
                    }
                case OpCode.JMPLE_L:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        return ExecuteJump(x1 <= x2, instruction.TokenI32);
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
                        if (!x.Script.Equals(context.Script)) return false;
                        if (!ExecuteCall(x.Position)) return false;
                        break;
                    }
                case OpCode.ABORT:
                    {
                        return false;
                    }
                case OpCode.ASSERT:
                    {
                        if (!TryPop(out bool x)) return false;
                        if (!x) return false;
                        break;
                    }
                case OpCode.THROW:
                    {
                        return false;
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
                        if (InvocationStack.Count == 0 || context_pop.StaticFields != CurrentContext.StaticFields)
                        {
                            context_pop.StaticFields?.ClearReferences();
                        }
                        context_pop.LocalVariables?.ClearReferences();
                        context_pop.Arguments?.ClearReferences();
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
                        if (!TryPop(out int n)) return false;
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
                        if (!TryPop(out int n)) return false;
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
                        if (!TryPop(out int n)) return false;
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
                        if (!TryPop(out int n)) return false;
                        if (!context.EvaluationStack.Reverse(n)) return false;
                        break;
                    }

                //Slot
                case OpCode.INITSSLOT:
                    {
                        if (context.StaticFields != null) return false;
                        if (instruction.TokenU8 == 0) return false;
                        context.StaticFields = new Slot(instruction.TokenU8, ReferenceCounter);
                        break;
                    }
                case OpCode.INITSLOT:
                    {
                        if (context.LocalVariables != null || context.Arguments != null) return false;
                        if (instruction.TokenU16 == 0) return false;
                        if (instruction.TokenU8 > 0)
                        {
                            context.LocalVariables = new Slot(instruction.TokenU8, ReferenceCounter);
                        }
                        if (instruction.TokenU8_1 > 0)
                        {
                            StackItem[] items = new StackItem[instruction.TokenU8_1];
                            for (int i = 0; i < instruction.TokenU8_1; i++)
                                if (!TryPop(out items[i]))
                                    return false;
                            context.Arguments = new Slot(items, ReferenceCounter);
                        }
                        break;
                    }
                case OpCode.LDSFLD0:
                case OpCode.LDSFLD1:
                case OpCode.LDSFLD2:
                case OpCode.LDSFLD3:
                case OpCode.LDSFLD4:
                case OpCode.LDSFLD5:
                case OpCode.LDSFLD6:
                    {
                        if (!ExecuteLoadFromSlot(context.StaticFields, instruction.OpCode - OpCode.LDSFLD0))
                            return false;
                        break;
                    }
                case OpCode.LDSFLD:
                    {
                        if (!ExecuteLoadFromSlot(context.StaticFields, instruction.TokenU8)) return false;
                        break;
                    }
                case OpCode.STSFLD0:
                case OpCode.STSFLD1:
                case OpCode.STSFLD2:
                case OpCode.STSFLD3:
                case OpCode.STSFLD4:
                case OpCode.STSFLD5:
                case OpCode.STSFLD6:
                    {
                        if (!ExecuteStoreToSlot(context.StaticFields, instruction.OpCode - OpCode.STSFLD0))
                            return false;
                        break;
                    }
                case OpCode.STSFLD:
                    {
                        if (!ExecuteStoreToSlot(context.StaticFields, instruction.TokenU8)) return false;
                        break;
                    }
                case OpCode.LDLOC0:
                case OpCode.LDLOC1:
                case OpCode.LDLOC2:
                case OpCode.LDLOC3:
                case OpCode.LDLOC4:
                case OpCode.LDLOC5:
                case OpCode.LDLOC6:
                    {
                        if (!ExecuteLoadFromSlot(context.LocalVariables, instruction.OpCode - OpCode.LDLOC0))
                            return false;
                        break;
                    }
                case OpCode.LDLOC:
                    {
                        if (!ExecuteLoadFromSlot(context.LocalVariables, instruction.TokenU8)) return false;
                        break;
                    }
                case OpCode.STLOC0:
                case OpCode.STLOC1:
                case OpCode.STLOC2:
                case OpCode.STLOC3:
                case OpCode.STLOC4:
                case OpCode.STLOC5:
                case OpCode.STLOC6:
                    {
                        if (!ExecuteStoreToSlot(context.LocalVariables, instruction.OpCode - OpCode.STLOC0))
                            return false;
                        break;
                    }
                case OpCode.STLOC:
                    {
                        if (!ExecuteStoreToSlot(context.LocalVariables, instruction.TokenU8)) return false;
                        break;
                    }
                case OpCode.LDARG0:
                case OpCode.LDARG1:
                case OpCode.LDARG2:
                case OpCode.LDARG3:
                case OpCode.LDARG4:
                case OpCode.LDARG5:
                case OpCode.LDARG6:
                    {
                        if (!ExecuteLoadFromSlot(context.Arguments, instruction.OpCode - OpCode.LDARG0))
                            return false;
                        break;
                    }
                case OpCode.LDARG:
                    {
                        if (!ExecuteLoadFromSlot(context.Arguments, instruction.TokenU8)) return false;
                        break;
                    }
                case OpCode.STARG0:
                case OpCode.STARG1:
                case OpCode.STARG2:
                case OpCode.STARG3:
                case OpCode.STARG4:
                case OpCode.STARG5:
                case OpCode.STARG6:
                    {
                        if (!ExecuteStoreToSlot(context.Arguments, instruction.OpCode - OpCode.STARG0))
                            return false;
                        break;
                    }
                case OpCode.STARG:
                    {
                        if (!ExecuteStoreToSlot(context.Arguments, instruction.TokenU8)) return false;
                        break;
                    }

                // Splice
                case OpCode.NEWBUFFER:
                    {
                        if (!TryPop(out int n)) return false;
                        if (n < 0 || n > MaxItemSize) return false;
                        Push(new Buffer(n));
                        break;
                    }
                case OpCode.MEMCPY:
                    {
                        if (!TryPop(out int n)) return false;
                        if (n < 0) return false;
                        if (!TryPop(out int si)) return false;
                        if (si < 0) return false;
                        if (!TryPop(out ReadOnlySpan<byte> src)) return false;
                        if (checked(si + n) > src.Length) return false;
                        if (!TryPop(out int di)) return false;
                        if (di < 0) return false;
                        if (!TryPop(out Buffer dst)) return false;
                        if (checked(di + n) > dst.Size) return false;
                        src.Slice(si, n).CopyTo(dst.InnerBuffer.AsSpan(di));
                        break;
                    }
                case OpCode.CAT:
                    {
                        if (!TryPop(out ReadOnlySpan<byte> x2)) return false;
                        if (!TryPop(out ReadOnlySpan<byte> x1)) return false;
                        int length = x1.Length + x2.Length;
                        if (!CheckMaxItemSize(length)) return false;
                        Buffer result = new Buffer(length);
                        x1.CopyTo(result.InnerBuffer);
                        x2.CopyTo(result.InnerBuffer.AsSpan(x1.Length));
                        Push(result);
                        break;
                    }
                case OpCode.SUBSTR:
                    {
                        if (!TryPop(out int count)) return false;
                        if (count < 0) return false;
                        if (!TryPop(out int index)) return false;
                        if (index < 0) return false;
                        if (!TryPop(out ReadOnlySpan<byte> x)) return false;
                        if (index + count > x.Length) return false;
                        Buffer result = new Buffer(count);
                        x.Slice(index, count).CopyTo(result.InnerBuffer);
                        Push(result);
                        break;
                    }
                case OpCode.LEFT:
                    {
                        if (!TryPop(out int count)) return false;
                        if (count < 0) return false;
                        if (!TryPop(out ReadOnlySpan<byte> x)) return false;
                        if (count > x.Length) return false;
                        Buffer result = new Buffer(count);
                        x[..count].CopyTo(result.InnerBuffer);
                        Push(result);
                        break;
                    }
                case OpCode.RIGHT:
                    {
                        if (!TryPop(out int count)) return false;
                        if (count < 0) return false;
                        if (!TryPop(out ReadOnlySpan<byte> x)) return false;
                        if (count > x.Length) return false;
                        Buffer result = new Buffer(count);
                        x[^count..^0].CopyTo(result.InnerBuffer);
                        Push(result);
                        break;
                    }

                // Bitwise logic
                case OpCode.INVERT:
                    {
                        if (!TryPop(out BigInteger x)) return false;
                        Push(~x);
                        break;
                    }
                case OpCode.AND:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        Push(x1 & x2);
                        break;
                    }
                case OpCode.OR:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        Push(x1 | x2);
                        break;
                    }
                case OpCode.XOR:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        Push(x1 ^ x2);
                        break;
                    }
                case OpCode.EQUAL:
                    {
                        if (!TryPop(out StackItem x2)) return false;
                        if (!TryPop(out StackItem x1)) return false;
                        Push(x1.Equals(x2));
                        break;
                    }
                case OpCode.NOTEQUAL:
                    {
                        if (!TryPop(out StackItem x2)) return false;
                        if (!TryPop(out StackItem x1)) return false;
                        Push(!x1.Equals(x2));
                        break;
                    }

                // Numeric
                case OpCode.SIGN:
                    {
                        if (!TryPop(out BigInteger x)) return false;
                        Push(x.Sign);
                        break;
                    }
                case OpCode.ABS:
                    {
                        if (!TryPop(out BigInteger x)) return false;
                        Push(BigInteger.Abs(x));
                        break;
                    }
                case OpCode.NEGATE:
                    {
                        if (!TryPop(out BigInteger x)) return false;
                        Push(-x);
                        break;
                    }
                case OpCode.INC:
                    {
                        if (!TryPop(out BigInteger x)) return false;
                        Push(x + 1);
                        break;
                    }
                case OpCode.DEC:
                    {
                        if (!TryPop(out BigInteger x)) return false;
                        Push(x - 1);
                        break;
                    }
                case OpCode.ADD:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        Push(x1 + x2);
                        break;
                    }
                case OpCode.SUB:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        Push(x1 - x2);
                        break;
                    }
                case OpCode.MUL:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        Push(x1 * x2);
                        break;
                    }
                case OpCode.DIV:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        Push(x1 / x2);
                        break;
                    }
                case OpCode.MOD:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        Push(x1 % x2);
                        break;
                    }
                case OpCode.SHL:
                    {
                        if (!TryPop(out int shift)) return false;
                        if (!CheckShift(shift)) return false;
                        if (shift == 0) break;
                        if (!TryPop(out BigInteger x)) return false;
                        Push(x << shift);
                        break;
                    }
                case OpCode.SHR:
                    {
                        if (!TryPop(out int shift)) return false;
                        if (!CheckShift(shift)) return false;
                        if (shift == 0) break;
                        if (!TryPop(out BigInteger x)) return false;
                        Push(x >> shift);
                        break;
                    }
                case OpCode.NOT:
                    {
                        if (!TryPop(out bool x)) return false;
                        Push(!x);
                        break;
                    }
                case OpCode.BOOLAND:
                    {
                        if (!TryPop(out bool x2)) return false;
                        if (!TryPop(out bool x1)) return false;
                        Push(x1 && x2);
                        break;
                    }
                case OpCode.BOOLOR:
                    {
                        if (!TryPop(out bool x2)) return false;
                        if (!TryPop(out bool x1)) return false;
                        Push(x1 || x2);
                        break;
                    }
                case OpCode.NZ:
                    {
                        if (!TryPop(out BigInteger x)) return false;
                        Push(!x.IsZero);
                        break;
                    }
                case OpCode.NUMEQUAL:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        Push(x1 == x2);
                        break;
                    }
                case OpCode.NUMNOTEQUAL:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        Push(x1 != x2);
                        break;
                    }
                case OpCode.LT:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        Push(x1 < x2);
                        break;
                    }
                case OpCode.LE:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        Push(x1 <= x2);
                        break;
                    }
                case OpCode.GT:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        Push(x1 > x2);
                        break;
                    }
                case OpCode.GE:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        Push(x1 >= x2);
                        break;
                    }
                case OpCode.MIN:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        Push(BigInteger.Min(x1, x2));
                        break;
                    }
                case OpCode.MAX:
                    {
                        if (!TryPop(out BigInteger x2)) return false;
                        if (!TryPop(out BigInteger x1)) return false;
                        Push(BigInteger.Max(x1, x2));
                        break;
                    }
                case OpCode.WITHIN:
                    {
                        if (!TryPop(out BigInteger b)) return false;
                        if (!TryPop(out BigInteger a)) return false;
                        if (!TryPop(out BigInteger x)) return false;
                        Push(a <= x && x < b);
                        break;
                    }

                // Compound-type
                case OpCode.PACK:
                    {
                        if (!TryPop(out int size)) return false;
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
                case OpCode.NEWARRAY0:
                    {
                        Push(new VMArray(ReferenceCounter));
                        break;
                    }
                case OpCode.NEWARRAY:
                case OpCode.NEWARRAY_T:
                    {
                        if (!TryPop(out int n)) return false;
                        if (n < 0 || n > MaxStackSize) return false;
                        StackItem item;
                        if (instruction.OpCode == OpCode.NEWARRAY_T)
                        {
                            StackItemType type = (StackItemType)instruction.TokenU8;
                            if (!Enum.IsDefined(typeof(StackItemType), type)) return false;
                            item = instruction.TokenU8 switch
                            {
                                (byte)StackItemType.Boolean => StackItem.False,
                                (byte)StackItemType.Integer => Integer.Zero,
                                (byte)StackItemType.ByteString => ByteString.Empty,
                                _ => StackItem.Null
                            };
                        }
                        else
                        {
                            item = StackItem.Null;
                        }
                        Push(new VMArray(ReferenceCounter, Enumerable.Repeat(item, n)));
                        break;
                    }
                case OpCode.NEWSTRUCT0:
                    {
                        Push(new Struct(ReferenceCounter));
                        break;
                    }
                case OpCode.NEWSTRUCT:
                    {
                        if (!TryPop(out int n)) return false;
                        if (n < 0 || n > MaxStackSize) return false;
                        Struct result = new Struct(ReferenceCounter);
                        for (var i = 0; i < n; i++)
                            result.Add(StackItem.Null);
                        Push(result);
                        break;
                    }
                case OpCode.NEWMAP:
                    {
                        Push(new Map(ReferenceCounter));
                        break;
                    }
                case OpCode.SIZE:
                    {
                        if (!TryPop(out StackItem x)) return false;
                        switch (x)
                        {
                            case CompoundType compound:
                                Push(compound.Count);
                                break;
                            case PrimitiveType primitive:
                                Push(primitive.Size);
                                break;
                            case Buffer buffer:
                                Push(buffer.Size);
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
                                {
                                    int index = key.ToInt32();
                                    if (index < 0) return false;
                                    Push(index < array.Count);
                                    break;
                                }
                            case Map map:
                                {
                                    Push(map.ContainsKey(key));
                                    break;
                                }
                            case Buffer buffer:
                                {
                                    int index = key.ToInt32();
                                    if (index < 0) return false;
                                    Push(index < buffer.Size);
                                    break;
                                }
                            case ByteString array:
                                {
                                    int index = key.ToInt32();
                                    if (index < 0) return false;
                                    Push(index < array.Size);
                                    break;
                                }
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
                case OpCode.PICKITEM:
                    {
                        if (!TryPop(out PrimitiveType key)) return false;
                        if (!TryPop(out StackItem x)) return false;
                        switch (x)
                        {
                            case VMArray array:
                                {
                                    int index = key.ToInt32();
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
                                    ReadOnlySpan<byte> byteArray = primitive.Span;
                                    int index = key.ToInt32();
                                    if (index < 0 || index >= byteArray.Length) return false;
                                    Push((BigInteger)byteArray[index]);
                                    break;
                                }
                            case Buffer buffer:
                                {
                                    int index = key.ToInt32();
                                    if (index < 0 || index >= buffer.Size) return false;
                                    Push((BigInteger)buffer.InnerBuffer[index]);
                                    break;
                                }
                            default:
                                return false;
                        }
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
                                    int index = key.ToInt32();
                                    if (index < 0 || index >= array.Count) return false;
                                    array[index] = value;
                                    break;
                                }
                            case Map map:
                                {
                                    map[key] = value;
                                    break;
                                }
                            case Buffer buffer:
                                {
                                    int index = key.ToInt32();
                                    if (index < 0 || index >= buffer.Size) return false;
                                    if (!(value is PrimitiveType p)) return false;
                                    int b = p.ToInt32();
                                    if (b < sbyte.MinValue || b > byte.MaxValue) return false;
                                    buffer.InnerBuffer[index] = (byte)b;
                                    break;
                                }
                            default:
                                return false;
                        }
                        break;
                    }
                case OpCode.REVERSEITEMS:
                    {
                        if (!TryPop(out StackItem x)) return false;
                        switch (x)
                        {
                            case VMArray array:
                                array.Reverse();
                                break;
                            case Buffer buffer:
                                Array.Reverse(buffer.InnerBuffer);
                                break;
                            default:
                                return false;
                        }
                        break;
                    }
                case OpCode.REMOVE:
                    {
                        if (!TryPop(out PrimitiveType key)) return false;
                        if (!TryPop(out StackItem x)) return false;
                        switch (x)
                        {
                            case VMArray array:
                                int index = key.ToInt32();
                                if (index < 0 || index >= array.Count) return false;
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
                case OpCode.CLEARITEMS:
                    {
                        if (!TryPop(out CompoundType x)) return false;
                        x.Clear();
                        break;
                    }

                //Types
                case OpCode.ISNULL:
                    {
                        if (!TryPop(out StackItem x)) return false;
                        Push(x.IsNull);
                        break;
                    }
                case OpCode.ISTYPE:
                    {
                        if (!TryPop(out StackItem x)) return false;
                        StackItemType type = (StackItemType)instruction.TokenU8;
                        if (type == StackItemType.Any || !Enum.IsDefined(typeof(StackItemType), type))
                            return false;
                        Push(x.Type == type);
                        break;
                    }
                case OpCode.CONVERT:
                    {
                        if (!TryPop(out StackItem x)) return false;
                        Push(x.ConvertTo((StackItemType)instruction.TokenU8));
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

        private bool ExecuteLoadFromSlot(Slot slot, int index)
        {
            if (slot is null) return false;
            if (index < 0 || index >= slot.Count) return false;
            Push(slot[index]);
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

        private bool ExecuteStoreToSlot(Slot slot, int index)
        {
            if (slot is null) return false;
            if (index < 0 || index >= slot.Count) return false;
            if (!TryPop(out StackItem item)) return false;
            slot[index] = item;
            return true;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out bool b)
        {
            if (TryPop(out StackItem item))
            {
                b = item.ToBoolean();
                return true;
            }
            else
            {
                b = default;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out ReadOnlySpan<byte> b)
        {
            if (!CurrentContext.EvaluationStack.TryPeek(out StackItem item))
            {
                b = default;
                return false;
            }
            switch (item)
            {
                case PrimitiveType primitive:
                    CurrentContext.EvaluationStack.Pop();
                    b = primitive.Span;
                    return true;
                case Buffer buffer:
                    CurrentContext.EvaluationStack.Pop();
                    b = buffer.InnerBuffer;
                    return true;
                default:
                    b = default;
                    return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out BigInteger i)
        {
            if (TryPop(out PrimitiveType item))
            {
                i = item.ToBigInteger();
                return true;
            }
            else
            {
                i = default;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out int i)
        {
            if (!CurrentContext.EvaluationStack.TryPeek(out PrimitiveType item))
            {
                i = default;
                return false;
            }
            BigInteger bi = item.ToBigInteger();
            if (bi < int.MinValue || bi > int.MaxValue)
            {
                i = default;
                return false;
            }
            CurrentContext.EvaluationStack.Pop();
            i = (int)bi;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out uint i)
        {
            if (!CurrentContext.EvaluationStack.TryPeek(out PrimitiveType item))
            {
                i = default;
                return false;
            }
            BigInteger bi = item.ToBigInteger();
            if (bi < uint.MinValue || bi > uint.MaxValue)
            {
                i = default;
                return false;
            }
            CurrentContext.EvaluationStack.Pop();
            i = (uint)bi;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPopInterface<T>(out T result) where T : class
        {
            if (!CurrentContext.EvaluationStack.TryPeek(out InteropInterface item))
            {
                result = default;
                return false;
            }
            if (!item.TryGetInterface(out result)) return false;
            CurrentContext.EvaluationStack.Pop();
            return true;
        }
    }
}
