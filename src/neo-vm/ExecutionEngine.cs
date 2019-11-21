using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public virtual int Max_SHL_SHR => 256;

        /// <summary>
        /// Min value for SHL and SHR
        /// </summary>
        public virtual int Min_SHL_SHR => -256;

        /// <summary>
        /// The max size in bytes allowed size for BigInteger
        /// </summary>
        public const int MaxSizeForBigInteger = 32;

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

        /// <summary>
        /// Set Max Array Size
        /// </summary>
        public virtual uint MaxArraySize => 1024;

        #endregion

        private int stackitem_count = 0;
        private bool is_stackitem_count_strict = true;

        public RandomAccessStack<ExecutionContext> InvocationStack { get; } = new RandomAccessStack<ExecutionContext>();
        public RandomAccessStack<StackItem> ResultStack { get; } = new RandomAccessStack<StackItem>();

        public ExecutionContext CurrentContext => InvocationStack.Count > 0 ? InvocationStack.Peek() : null;
        public ExecutionContext EntryContext => InvocationStack.Count > 0 ? InvocationStack.Peek(InvocationStack.Count - 1) : null;
        public VMState State { get; internal protected set; } = VMState.BREAK;

        #region Limits

        /// <summary>
        /// Check if it is possible to overflow the MaxArraySize
        /// </summary>
        /// <param name="length">Length</param>
        /// <returns>Return True if are allowed, otherwise False</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckArraySize(int length) => length <= MaxArraySize;

        /// <summary>
        /// Check if the is possible to overflow the MaxItemSize
        /// </summary>
        /// <param name="length">Length</param>
        /// <returns>Return True if are allowed, otherwise False</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckMaxItemSize(int length) => length >= 0 && length <= MaxItemSize;

        /// <summary>
        /// Check if the BigInteger is allowed for numeric operations
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns>Return True if are allowed, otherwise False</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckBigInteger(BigInteger value) => value.GetByteCount() <= MaxSizeForBigInteger;

        /// <summary>
        /// Check if the number is allowed from SHL and SHR
        /// </summary>
        /// <param name="shift">Shift</param>
        /// <returns>Return True if are allowed, otherwise False</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckShift(int shift) => shift <= Max_SHL_SHR && shift >= Min_SHL_SHR;

        /// <summary>
        /// Check if the is possible to overflow the MaxStackSize
        /// </summary>
        /// <param name="strict">Is stack count strict?</param>
        /// <param name="count">Stack item count</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CheckStackSize(bool strict, int count = 1)
        {
            is_stackitem_count_strict &= strict;
            stackitem_count += count;

            if (stackitem_count < 0) stackitem_count = int.MaxValue;
            if (stackitem_count <= MaxStackSize) return true;
            if (is_stackitem_count_strict) return false;

            // Deep inspect

            stackitem_count = GetItemCount(InvocationStack.Select(p => p.EvaluationStack).Distinct().Concat(InvocationStack.Select(p => p.AltStack).Distinct()).SelectMany(p => p));
            if (stackitem_count > MaxStackSize) return false;
            is_stackitem_count_strict = true;

            return true;
        }

        /// <summary>
        /// Get item count
        /// </summary>
        /// <param name="items">Items</param>
        /// <returns>Return the number of items</returns>
        private static int GetItemCount(IEnumerable<StackItem> items)
        {
            Queue<StackItem> queue = new Queue<StackItem>(items);
            List<StackItem> counted = new List<StackItem>();
            int count = 0;
            while (queue.Count > 0)
            {
                StackItem item = queue.Dequeue();
                count++;
                switch (item)
                {
                    case Types.Array array:
                        {
                            if (counted.Any(p => ReferenceEquals(p, array)))
                                continue;
                            counted.Add(array);
                            foreach (StackItem subitem in array)
                                queue.Enqueue(subitem);
                            break;
                        }
                    case Map map:
                        {
                            if (counted.Any(p => ReferenceEquals(p, map)))
                                continue;
                            counted.Add(map);
                            count += map.Count; // Count key items
                            foreach (StackItem subitem in map.Values)
                                queue.Enqueue(subitem);
                            break;
                        }
                }
            }
            return count;
        }

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

        private bool ExecuteInstruction()
        {
            ExecutionContext context = CurrentContext;
            Instruction instruction = context.CurrentInstruction;
            if (instruction.OpCode >= OpCode.PUSHBYTES1 && instruction.OpCode <= OpCode.PUSHDATA4)
            {
                if (!CheckMaxItemSize(instruction.Operand.Length)) return false;
                context.EvaluationStack.Push(instruction.Operand);
                if (!CheckStackSize(true)) return false;
            }
            else switch (instruction.OpCode)
                {
                    // Push value
                    case OpCode.PUSH0:
                        {
                            context.EvaluationStack.Push(ReadOnlyMemory<byte>.Empty);
                            if (!CheckStackSize(true)) return false;
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
                            context.EvaluationStack.Push((int)instruction.OpCode - (int)OpCode.PUSH1 + 1);
                            if (!CheckStackSize(true)) return false;
                            break;
                        }
                    case OpCode.PUSHNULL:
                        {
                            context.EvaluationStack.Push(StackItem.Null);
                            if (!CheckStackSize(true)) return false;
                            break;
                        }

                    // Control
                    case OpCode.NOP: break;
                    case OpCode.JMP:
                    case OpCode.JMPIF:
                    case OpCode.JMPIFNOT:
                        {
                            int offset = context.InstructionPointer + instruction.TokenI16;
                            if (offset < 0 || offset > context.Script.Length) return false;
                            bool fValue = true;
                            if (instruction.OpCode > OpCode.JMP)
                            {
                                CheckStackSize(false, -1);
                                fValue = context.EvaluationStack.Pop().ToBoolean();

                                if (instruction.OpCode == OpCode.JMPIFNOT)
                                    fValue = !fValue;
                            }
                            if (fValue)
                                context.InstructionPointer = offset;
                            else
                                context.InstructionPointer += 3;
                            return true;
                        }
                    case OpCode.CALL:
                        {
                            ExecutionContext context_call = context.Clone();
                            context_call.InstructionPointer = context.InstructionPointer + instruction.TokenI16;
                            if (context_call.InstructionPointer < 0 || context_call.InstructionPointer > context_call.Script.Length) return false;
                            LoadContext(context_call);
                            break;
                        }
                    case OpCode.RET:
                        {
                            ExecutionContext context_pop = InvocationStack.Pop();
                            int rvcount = context_pop.RVCount;
                            if (rvcount == -1) rvcount = context_pop.EvaluationStack.Count;
                            if (rvcount > 0)
                            {
                                if (context_pop.EvaluationStack.Count < rvcount) return false;
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
                            CheckStackSize(false, 0);
                            if (InvocationStack.Count == 0)
                            {
                                State = VMState.HALT;
                            }
                            ContextUnloaded(context_pop);
                            return true;
                        }
                    case OpCode.SYSCALL:
                        {
                            if (!OnSysCall(instruction.TokenU32) || !CheckStackSize(false, int.MaxValue))
                                return false;
                            break;
                        }

                    // Stack ops
                    case OpCode.DUPFROMALTSTACKBOTTOM:
                        {
                            var item = context.AltStack.Peek(context.AltStack.Count - 1);
                            context.EvaluationStack.Push(item);
                            if (!CheckStackSize(true)) return false;
                            break;
                        }
                    case OpCode.DUPFROMALTSTACK:
                        {
                            context.EvaluationStack.Push(context.AltStack.Peek());
                            if (!CheckStackSize(true)) return false;
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
                    case OpCode.ISNULL:
                        {
                            bool b = context.EvaluationStack.Peek().IsNull;
                            context.EvaluationStack.Set(0, b);
                            CheckStackSize(false, 0);
                            break;
                        }
                    case OpCode.XDROP:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_n))
                                return false;
                            int n = (int)item_n.ToBigInteger();
                            if (n < 0) return false;
                            context.EvaluationStack.Remove(n);
                            CheckStackSize(false, -2);
                            break;
                        }
                    case OpCode.XSWAP:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_n))
                                return false;
                            int n = (int)item_n.ToBigInteger();
                            if (n < 0) return false;
                            CheckStackSize(true, -1);
                            if (n == 0) break;
                            StackItem xn = context.EvaluationStack.Peek(n);
                            context.EvaluationStack.Set(n, context.EvaluationStack.Peek());
                            context.EvaluationStack.Set(0, xn);
                            break;
                        }
                    case OpCode.XTUCK:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_n))
                                return false;
                            int n = (int)item_n.ToBigInteger();
                            if (n <= 0) return false;
                            context.EvaluationStack.Insert(n, context.EvaluationStack.Peek());
                            break;
                        }
                    case OpCode.DEPTH:
                        {
                            context.EvaluationStack.Push(context.EvaluationStack.Count);
                            if (!CheckStackSize(true)) return false;
                            break;
                        }
                    case OpCode.DROP:
                        {
                            context.EvaluationStack.Pop();
                            CheckStackSize(false, -1);
                            break;
                        }
                    case OpCode.DUP:
                        {
                            context.EvaluationStack.Push(context.EvaluationStack.Peek());
                            if (!CheckStackSize(true)) return false;
                            break;
                        }
                    case OpCode.NIP:
                        {
                            context.EvaluationStack.Remove(1);
                            CheckStackSize(false, -1);
                            break;
                        }
                    case OpCode.OVER:
                        {
                            context.EvaluationStack.Push(context.EvaluationStack.Peek(1));
                            if (!CheckStackSize(true)) return false;
                            break;
                        }
                    case OpCode.PICK:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_n))
                                return false;
                            int n = (int)item_n.ToBigInteger();
                            if (n < 0) return false;
                            context.EvaluationStack.Push(context.EvaluationStack.Peek(n));
                            break;
                        }
                    case OpCode.ROLL:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_n))
                                return false;
                            int n = (int)item_n.ToBigInteger();
                            if (n < 0) return false;
                            CheckStackSize(true, -1);
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
                            context.EvaluationStack.Insert(2, context.EvaluationStack.Peek());
                            if (!CheckStackSize(true)) return false;
                            break;
                        }
                    case OpCode.CAT:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x2))
                                return false;
                            ReadOnlyMemory<byte> x2 = item_x2.ToMemory();
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x1))
                                return false;
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
                            context.EvaluationStack.Push(result);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.SUBSTR:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_count))
                                return false;
                            int count = (int)item_count.ToBigInteger();
                            if (count < 0) return false;
                            if (count > MaxItemSize) count = (int)MaxItemSize;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_index))
                                return false;
                            int index = (int)item_index.ToBigInteger();
                            if (index < 0) return false;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x))
                                return false;
                            ReadOnlyMemory<byte> x = item_x.ToMemory();
                            if (index > x.Length) return false;
                            if (index + count > x.Length) count = x.Length - index;
                            context.EvaluationStack.Push(x.Slice(index, count));
                            CheckStackSize(true, -2);
                            break;
                        }
                    case OpCode.LEFT:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_count))
                                return false;
                            int count = (int)item_count.ToBigInteger();
                            if (count < 0) return false;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x))
                                return false;
                            ReadOnlyMemory<byte> x = item_x.ToMemory();
                            if (count < x.Length)
                                x = x[0..count];
                            context.EvaluationStack.Push(x);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.RIGHT:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_count))
                                return false;
                            int count = (int)item_count.ToBigInteger();
                            if (count < 0) return false;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x))
                                return false;
                            ReadOnlyMemory<byte> x = item_x.ToMemory();
                            if (count > x.Length) return false;
                            if (count < x.Length)
                                x = x[^count..^0];
                            context.EvaluationStack.Push(x);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.SIZE:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType x))
                                return false;
                            context.EvaluationStack.Push(x.GetByteLength());
                            break;
                        }

                    // Bitwise logic
                    case OpCode.INVERT:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x))
                                return false;
                            BigInteger x = item_x.ToBigInteger();
                            if (!CheckBigInteger(x)) return false;
                            context.EvaluationStack.Push(~x);
                            break;
                        }
                    case OpCode.AND:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x2))
                                return false;
                            BigInteger x2 = item_x2.ToBigInteger();
                            if (!CheckBigInteger(x2)) return false;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x1))
                                return false;
                            BigInteger x1 = item_x1.ToBigInteger();
                            if (!CheckBigInteger(x1)) return false;
                            context.EvaluationStack.Push(x1 & x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.OR:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x2))
                                return false;
                            BigInteger x2 = item_x2.ToBigInteger();
                            if (!CheckBigInteger(x2)) return false;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x1))
                                return false;
                            BigInteger x1 = item_x1.ToBigInteger();
                            if (!CheckBigInteger(x1)) return false;
                            context.EvaluationStack.Push(x1 | x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.XOR:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x2))
                                return false;
                            BigInteger x2 = item_x2.ToBigInteger();
                            if (!CheckBigInteger(x2)) return false;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x1))
                                return false;
                            BigInteger x1 = item_x1.ToBigInteger();
                            if (!CheckBigInteger(x1)) return false;
                            context.EvaluationStack.Push(x1 ^ x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.EQUAL:
                        {
                            StackItem x2 = context.EvaluationStack.Pop();
                            StackItem x1 = context.EvaluationStack.Pop();
                            context.EvaluationStack.Push(x1.Equals(x2));
                            CheckStackSize(false, -1);
                            break;
                        }

                    // Numeric
                    case OpCode.INC:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x))
                                return false;
                            BigInteger x = item_x.ToBigInteger();
                            if (!CheckBigInteger(x)) return false;
                            x += 1;
                            if (!CheckBigInteger(x)) return false;
                            context.EvaluationStack.Push(x);
                            break;
                        }
                    case OpCode.DEC:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x))
                                return false;
                            BigInteger x = item_x.ToBigInteger();
                            if (!CheckBigInteger(x)) return false;
                            x -= 1;
                            if (!CheckBigInteger(x)) return false;
                            context.EvaluationStack.Push(x);
                            break;
                        }
                    case OpCode.SIGN:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x))
                                return false;
                            BigInteger x = item_x.ToBigInteger();
                            if (!CheckBigInteger(x)) return false;
                            context.EvaluationStack.Push(x.Sign);
                            break;
                        }
                    case OpCode.NEGATE:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x))
                                return false;
                            BigInteger x = item_x.ToBigInteger();
                            if (!CheckBigInteger(x)) return false;
                            context.EvaluationStack.Push(-x);
                            break;
                        }
                    case OpCode.ABS:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x))
                                return false;
                            BigInteger x = item_x.ToBigInteger();
                            if (!CheckBigInteger(x)) return false;
                            context.EvaluationStack.Push(BigInteger.Abs(x));
                            break;
                        }
                    case OpCode.NOT:
                        {
                            bool x = context.EvaluationStack.Pop().ToBoolean();
                            context.EvaluationStack.Push(!x);
                            CheckStackSize(false, 0);
                            break;
                        }
                    case OpCode.NZ:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x))
                                return false;
                            BigInteger x = item_x.ToBigInteger();
                            if (!CheckBigInteger(x)) return false;
                            context.EvaluationStack.Push(!x.IsZero);
                            break;
                        }
                    case OpCode.ADD:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x2))
                                return false;
                            BigInteger x2 = item_x2.ToBigInteger();
                            if (!CheckBigInteger(x2)) return false;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x1))
                                return false;
                            BigInteger x1 = item_x1.ToBigInteger();
                            if (!CheckBigInteger(x1)) return false;
                            BigInteger result = x1 + x2;
                            if (!CheckBigInteger(result)) return false;
                            context.EvaluationStack.Push(result);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.SUB:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x2))
                                return false;
                            BigInteger x2 = item_x2.ToBigInteger();
                            if (!CheckBigInteger(x2)) return false;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x1))
                                return false;
                            BigInteger x1 = item_x1.ToBigInteger();
                            if (!CheckBigInteger(x1)) return false;
                            BigInteger result = x1 - x2;
                            if (!CheckBigInteger(result)) return false;
                            context.EvaluationStack.Push(result);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.MUL:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x2))
                                return false;
                            BigInteger x2 = item_x2.ToBigInteger();
                            if (!CheckBigInteger(x2)) return false;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x1))
                                return false;
                            BigInteger x1 = item_x1.ToBigInteger();
                            if (!CheckBigInteger(x1)) return false;
                            BigInteger result = x1 * x2;
                            if (!CheckBigInteger(result)) return false;
                            context.EvaluationStack.Push(result);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.DIV:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x2))
                                return false;
                            BigInteger x2 = item_x2.ToBigInteger();
                            if (!CheckBigInteger(x2)) return false;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x1))
                                return false;
                            BigInteger x1 = item_x1.ToBigInteger();
                            if (!CheckBigInteger(x1)) return false;
                            context.EvaluationStack.Push(x1 / x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.MOD:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x2))
                                return false;
                            BigInteger x2 = item_x2.ToBigInteger();
                            if (!CheckBigInteger(x2)) return false;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x1))
                                return false;
                            BigInteger x1 = item_x1.ToBigInteger();
                            if (!CheckBigInteger(x1)) return false;
                            context.EvaluationStack.Push(x1 % x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.SHL:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_shift))
                                return false;
                            int shift = (int)item_shift.ToBigInteger();
                            CheckStackSize(true, -1);
                            if (!CheckShift(shift)) return false;
                            if (shift == 0) break;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x))
                                return false;
                            BigInteger x = item_x.ToBigInteger();
                            if (!CheckBigInteger(x)) return false;
                            x <<= shift;
                            if (!CheckBigInteger(x)) return false;
                            context.EvaluationStack.Push(x);
                            break;
                        }
                    case OpCode.SHR:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_shift))
                                return false;
                            int shift = (int)item_shift.ToBigInteger();
                            CheckStackSize(true, -1);
                            if (!CheckShift(shift)) return false;
                            if (shift == 0) break;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x))
                                return false;
                            BigInteger x = item_x.ToBigInteger();
                            if (!CheckBigInteger(x)) return false;
                            x >>= shift;
                            if (!CheckBigInteger(x)) return false;
                            context.EvaluationStack.Push(x);
                            break;
                        }
                    case OpCode.BOOLAND:
                        {
                            bool x2 = context.EvaluationStack.Pop().ToBoolean();
                            bool x1 = context.EvaluationStack.Pop().ToBoolean();
                            context.EvaluationStack.Push(x1 && x2);
                            CheckStackSize(false, -1);
                            break;
                        }
                    case OpCode.BOOLOR:
                        {
                            bool x2 = context.EvaluationStack.Pop().ToBoolean();
                            bool x1 = context.EvaluationStack.Pop().ToBoolean();
                            context.EvaluationStack.Push(x1 || x2);
                            CheckStackSize(false, -1);
                            break;
                        }
                    case OpCode.NUMEQUAL:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x2))
                                return false;
                            BigInteger x2 = item_x2.ToBigInteger();
                            if (!CheckBigInteger(x2)) return false;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x1))
                                return false;
                            BigInteger x1 = item_x1.ToBigInteger();
                            if (!CheckBigInteger(x1)) return false;
                            context.EvaluationStack.Push(x1 == x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.NUMNOTEQUAL:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x2))
                                return false;
                            BigInteger x2 = item_x2.ToBigInteger();
                            if (!CheckBigInteger(x2)) return false;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x1))
                                return false;
                            BigInteger x1 = item_x1.ToBigInteger();
                            if (!CheckBigInteger(x1)) return false;
                            context.EvaluationStack.Push(x1 != x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.LT:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x2))
                                return false;
                            BigInteger x2 = item_x2.ToBigInteger();
                            if (!CheckBigInteger(x2)) return false;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x1))
                                return false;
                            BigInteger x1 = item_x1.ToBigInteger();
                            if (!CheckBigInteger(x1)) return false;
                            context.EvaluationStack.Push(x1 < x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.GT:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x2))
                                return false;
                            BigInteger x2 = item_x2.ToBigInteger();
                            if (!CheckBigInteger(x2)) return false;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x1))
                                return false;
                            BigInteger x1 = item_x1.ToBigInteger();
                            if (!CheckBigInteger(x1)) return false;
                            context.EvaluationStack.Push(x1 > x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.LTE:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x2))
                                return false;
                            BigInteger x2 = item_x2.ToBigInteger();
                            if (!CheckBigInteger(x2)) return false;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x1))
                                return false;
                            BigInteger x1 = item_x1.ToBigInteger();
                            if (!CheckBigInteger(x1)) return false;
                            context.EvaluationStack.Push(x1 <= x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.GTE:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x2))
                                return false;
                            BigInteger x2 = item_x2.ToBigInteger();
                            if (!CheckBigInteger(x2)) return false;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x1))
                                return false;
                            BigInteger x1 = item_x1.ToBigInteger();
                            if (!CheckBigInteger(x1)) return false;
                            context.EvaluationStack.Push(x1 >= x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.MIN:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x2))
                                return false;
                            BigInteger x2 = item_x2.ToBigInteger();
                            if (!CheckBigInteger(x2)) return false;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x1))
                                return false;
                            BigInteger x1 = item_x1.ToBigInteger();
                            if (!CheckBigInteger(x1)) return false;
                            context.EvaluationStack.Push(BigInteger.Min(x1, x2));
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.MAX:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x2))
                                return false;
                            BigInteger x2 = item_x2.ToBigInteger();
                            if (!CheckBigInteger(x2)) return false;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x1))
                                return false;
                            BigInteger x1 = item_x1.ToBigInteger();
                            if (!CheckBigInteger(x1)) return false;
                            context.EvaluationStack.Push(BigInteger.Max(x1, x2));
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.WITHIN:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_b))
                                return false;
                            BigInteger b = item_b.ToBigInteger();
                            if (!CheckBigInteger(b)) return false;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_a))
                                return false;
                            BigInteger a = item_a.ToBigInteger();
                            if (!CheckBigInteger(a)) return false;
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_x))
                                return false;
                            BigInteger x = item_x.ToBigInteger();
                            if (!CheckBigInteger(x)) return false;
                            context.EvaluationStack.Push(a <= x && x < b);
                            CheckStackSize(true, -2);
                            break;
                        }

                    // Array
                    case OpCode.ARRAYSIZE:
                        {
                            switch (context.EvaluationStack.Pop())
                            {
                                case CompoundType compound:
                                    context.EvaluationStack.Push(compound.Count);
                                    CheckStackSize(false, 0);
                                    break;
                                case PrimitiveType primitive:
                                    context.EvaluationStack.Push(primitive.GetByteLength());
                                    CheckStackSize(true, 0);
                                    break;
                                default:
                                    return false;
                            }
                            break;
                        }
                    case OpCode.PACK:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType item_size))
                                return false;
                            int size = (int)item_size.ToBigInteger();
                            if (size < 0 || size > context.EvaluationStack.Count || !CheckArraySize(size))
                                return false;
                            List<StackItem> items = new List<StackItem>(size);
                            for (int i = 0; i < size; i++)
                                items.Add(context.EvaluationStack.Pop());
                            context.EvaluationStack.Push(items);
                            break;
                        }
                    case OpCode.UNPACK:
                        {
                            if (!context.EvaluationStack.TryPop(out VMArray array))
                                return false;
                            for (int i = array.Count - 1; i >= 0; i--)
                                context.EvaluationStack.Push(array[i]);
                            context.EvaluationStack.Push(array.Count);
                            if (!CheckStackSize(false, array.Count)) return false;
                            break;
                        }
                    case OpCode.PICKITEM:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType key))
                                return false;
                            switch (context.EvaluationStack.Pop())
                            {
                                case VMArray array:
                                    {
                                        int index = (int)key.ToBigInteger();
                                        if (index < 0 || index >= array.Count) return false;
                                        context.EvaluationStack.Push(array[index]);
                                        CheckStackSize(false, -1);
                                        break;
                                    }
                                case Map map:
                                    {
                                        if (!map.TryGetValue(key, out StackItem value)) return false;
                                        context.EvaluationStack.Push(value);
                                        CheckStackSize(false, -1);
                                        break;
                                    }
                                case PrimitiveType primitive:
                                    {
                                        ReadOnlySpan<byte> byteArray = primitive.ToByteArray();
                                        int index = (int)key.ToBigInteger();
                                        if (index < 0 || index >= byteArray.Length) return false;
                                        context.EvaluationStack.Push((int)byteArray[index]);
                                        CheckStackSize(true, -1);
                                        break;
                                    }
                                default:
                                    return false;
                            }
                            break;
                        }
                    case OpCode.SETITEM:
                        {
                            StackItem value = context.EvaluationStack.Pop();
                            if (value is Struct s) value = s.Clone();
                            if (!context.EvaluationStack.TryPop(out PrimitiveType key))
                                return false;
                            switch (context.EvaluationStack.Pop())
                            {
                                case VMArray array:
                                    {
                                        int index = (int)key.ToBigInteger();
                                        if (index < 0 || index >= array.Count) return false;
                                        array[index] = value;
                                        break;
                                    }
                                case Map map:
                                    {
                                        if (!map.ContainsKey(key) && !CheckArraySize(map.Count + 1))
                                            return false;
                                        map[key] = value;
                                        break;
                                    }
                                default:
                                    return false;
                            }

                            if (!CheckStackSize(false, int.MaxValue))
                                return false;

                            break;
                        }
                    case OpCode.NEWARRAY:
                    case OpCode.NEWSTRUCT:
                        {
                            switch (context.EvaluationStack.Peek())
                            {
                                case VMArray array:
                                    {
                                        // Allow to convert between array and struct
                                        if (array is Struct)
                                        {
                                            if (instruction.OpCode == OpCode.NEWSTRUCT)
                                                break;
                                        }
                                        else
                                        {
                                            if (instruction.OpCode == OpCode.NEWARRAY)
                                                break;
                                        }
                                        VMArray result = instruction.OpCode == OpCode.NEWARRAY
                                            ? new VMArray(array)
                                            : new Struct(array);
                                        context.EvaluationStack.Set(0, result);
                                        if (!CheckStackSize(false, int.MaxValue)) return false;
                                    }
                                    break;
                                case PrimitiveType primitive:
                                    {
                                        int count = (int)primitive.ToBigInteger();
                                        if (count < 0 || !CheckArraySize(count)) return false;
                                        List<StackItem> items = new List<StackItem>(count);
                                        for (var i = 0; i < count; i++)
                                            items.Add(false);
                                        VMArray result = instruction.OpCode == OpCode.NEWARRAY
                                            ? new VMArray(items)
                                            : new Struct(items);
                                        context.EvaluationStack.Set(0, result);
                                        if (!CheckStackSize(true, count)) return false;
                                    }
                                    break;
                                default:
                                    return false;
                            }
                            break;
                        }
                    case OpCode.NEWMAP:
                        {
                            context.EvaluationStack.Push(new Map());
                            if (!CheckStackSize(true)) return false;
                            break;
                        }
                    case OpCode.APPEND:
                        {
                            StackItem newItem = context.EvaluationStack.Pop();
                            if (newItem is Struct s) newItem = s.Clone();
                            StackItem arrItem = context.EvaluationStack.Pop();
                            if (!(arrItem is VMArray array)) return false;
                            if (!CheckArraySize(array.Count + 1)) return false;
                            array.Add(newItem);
                            if (!CheckStackSize(false, int.MaxValue)) return false;
                            break;
                        }
                    case OpCode.REVERSE:
                        {
                            StackItem arrItem = context.EvaluationStack.Pop();
                            CheckStackSize(false, -1);
                            if (!(arrItem is VMArray array)) return false;
                            array.Reverse();
                            break;
                        }
                    case OpCode.REMOVE:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType key))
                                return false;
                            StackItem value = context.EvaluationStack.Pop();
                            CheckStackSize(false, -2);
                            switch (value)
                            {
                                case VMArray array:
                                    int index = (int)key.ToBigInteger();
                                    if (index < 0 || index >= array.Count) return false;
                                    array.RemoveAt(index);
                                    CheckStackSize(false, -1);
                                    break;
                                case Map map:
                                    if (map.Remove(key))
                                        CheckStackSize(false, -2);
                                    break;
                                default:
                                    return false;
                            }
                            break;
                        }
                    case OpCode.HASKEY:
                        {
                            if (!context.EvaluationStack.TryPop(out PrimitiveType key))
                                return false;
                            switch (context.EvaluationStack.Pop())
                            {
                                case VMArray array:
                                    int index = (int)key.ToBigInteger();
                                    if (index < 0) return false;
                                    context.EvaluationStack.Push(index < array.Count);
                                    break;
                                case Map map:
                                    context.EvaluationStack.Push(map.ContainsKey(key));
                                    break;
                                default:
                                    return false;
                            }
                            CheckStackSize(false, -1);
                            break;
                        }
                    case OpCode.KEYS:
                        {
                            if (!context.EvaluationStack.TryPop(out Map map)) return false;
                            context.EvaluationStack.Push(new VMArray(map.Keys));
                            if (!CheckStackSize(false, map.Count)) return false;
                            break;
                        }
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
                                    return false;
                            }
                            List<StackItem> newArray = new List<StackItem>(values.Count);
                            foreach (StackItem item in values)
                                if (item is Struct s)
                                    newArray.Add(s.Clone());
                                else
                                    newArray.Add(item);
                            context.EvaluationStack.Push(new VMArray(newArray));
                            if (!CheckStackSize(false, int.MaxValue)) return false;
                            break;
                        }

                    // Exceptions
                    case OpCode.THROW:
                        {
                            return false;
                        }
                    case OpCode.THROWIFNOT:
                        {
                            if (!context.EvaluationStack.Pop().ToBoolean())
                                return false;
                            CheckStackSize(false, -1);
                            break;
                        }
                    default:
                        return false;
                }
            context.MoveNext();
            return true;
        }

        protected virtual void LoadContext(ExecutionContext context)
        {
            if (InvocationStack.Count >= MaxInvocationStackSize)
                throw new InvalidOperationException();
            InvocationStack.Push(context);
        }

        public ExecutionContext LoadScript(Script script, int rvcount = -1)
        {
            ExecutionContext context = new ExecutionContext(script, CurrentContext?.Script, rvcount);
            LoadContext(context);
            return context;
        }

        protected virtual bool OnSysCall(uint method) => false;

        protected virtual bool PostExecuteInstruction(Instruction instruction) => true;

        protected virtual bool PreExecuteInstruction() => true;
    }
}
