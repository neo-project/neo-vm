using Neo.VM.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
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

        private static readonly byte[] EmptyBytes = new byte[0];

        private int stackitem_count = 0;
        private bool is_stackitem_count_strict = true;

        private readonly IScriptTable table;

        public IScriptContainer ScriptContainer { get; }
        public ICrypto Crypto { get; }
        public IInteropService Service { get; }
        public RandomAccessStack<ExecutionContext> InvocationStack { get; } = new RandomAccessStack<ExecutionContext>();
        public RandomAccessStack<StackItem> ResultStack { get; } = new RandomAccessStack<StackItem>();
        public ExecutionContext CurrentContext => InvocationStack.Peek();
        public ExecutionContext CallingContext => InvocationStack.Count > 1 ? InvocationStack.Peek(1) : null;
        public ExecutionContext EntryContext => InvocationStack.Peek(InvocationStack.Count - 1);
        public VMState State { get; internal protected set; } = VMState.BREAK;

        public ExecutionEngine(IScriptContainer container, ICrypto crypto, IScriptTable table = null, IInteropService service = null)
        {
            this.ScriptContainer = container;
            this.Crypto = crypto;
            this.table = table;
            this.Service = service;
        }

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
        /// Check if the is possible to overflow the MaxInvocationStack
        /// </summary>
        /// <param name="stack">Stack</param>
        /// <returns>Return True if are allowed, otherwise False</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckMaxInvocationStack() => InvocationStack.Count < MaxInvocationStackSize;

        /// <summary>
        /// Check if the BigInteger is allowed for numeric operations
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns>Return True if are allowed, otherwise False</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckBigInteger(BigInteger value) => value.ToByteArray().Length <= MaxSizeForBigInteger;

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
        /// <param name="count">Stack item count</param>
        /// <param name="strict">Is stack count strict?</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckStackSize(bool strict, int count = 1)
        {
            is_stackitem_count_strict &= strict;
            stackitem_count += count;

            if (stackitem_count < 0) stackitem_count = int.MaxValue;
            if (stackitem_count <= MaxStackSize) return true;
            if (is_stackitem_count_strict) return false;

            // Deep inspect

            stackitem_count = GetItemCount(InvocationStack.SelectMany(p => p.EvaluationStack.Concat(p.AltStack)));
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
                            foreach (StackItem subitem in map.Values)
                                queue.Enqueue(subitem);
                            break;
                        }
                }
            }
            return count;
        }

        #endregion

        public virtual void Dispose()
        {
            InvocationStack.Clear();
        }

        public void Execute()
        {
            State &= ~VMState.BREAK;
            while (!State.HasFlag(VMState.HALT) && !State.HasFlag(VMState.FAULT) && !State.HasFlag(VMState.BREAK))
                ExecuteNext();
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
                            context.EvaluationStack.Push(EmptyBytes);
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
                                fValue = context.EvaluationStack.Pop().GetBoolean();

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
                            if (!CheckMaxInvocationStack()) return false;
                            ExecutionContext context_call = LoadScript(context.Script);
                            context_call.InstructionPointer = context.InstructionPointer + instruction.TokenI16;
                            if (context_call.InstructionPointer < 0 || context_call.InstructionPointer > context_call.Script.Length) return false;
                            context.EvaluationStack.CopyTo(context_call.EvaluationStack);
                            context.EvaluationStack.Clear();
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
                            return true;
                        }
                    case OpCode.APPCALL:
                    case OpCode.TAILCALL:
                        {
                            if (table == null || (instruction.OpCode == OpCode.APPCALL && !CheckMaxInvocationStack()))
                                return false;
                            byte[] script_hash = instruction.Operand;
                            if (!Unsafe.NotZero(script_hash))
                            {
                                script_hash = context.EvaluationStack.Pop().GetByteArray();
                            }
                            ExecutionContext context_new = LoadScriptByHash(script_hash);
                            if (context_new == null) return false;
                            context.EvaluationStack.CopyTo(context_new.EvaluationStack);
                            if (instruction.OpCode == OpCode.TAILCALL)
                                InvocationStack.Remove(1);
                            else
                                context.EvaluationStack.Clear();
                            CheckStackSize(false, 0);
                            break;
                        }
                    case OpCode.SYSCALL:
                        {
                            if (instruction.Operand.Length > 252) return false;
                            if (Service?.Invoke(instruction.Operand, this) != true || !CheckStackSize(false, int.MaxValue))
                                return false;
                            break;
                        }

                    // Stack ops
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
                    case OpCode.XDROP:
                        {
                            int n = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (n < 0) return false;
                            context.EvaluationStack.Remove(n);
                            CheckStackSize(false, -2);
                            break;
                        }
                    case OpCode.XSWAP:
                        {
                            int n = (int)context.EvaluationStack.Pop().GetBigInteger();
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
                            int n = (int)context.EvaluationStack.Pop().GetBigInteger();
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
                            int n = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (n < 0) return false;
                            context.EvaluationStack.Push(context.EvaluationStack.Peek(n));
                            break;
                        }
                    case OpCode.ROLL:
                        {
                            int n = (int)context.EvaluationStack.Pop().GetBigInteger();
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
                            byte[] x2 = context.EvaluationStack.Pop().GetByteArray();
                            byte[] x1 = context.EvaluationStack.Pop().GetByteArray();
                            byte[] result;
                            if (x1.Length == 0)
                            {
                                result = x2;
                            }
                            else if (x2.Length == 0)
                            {
                                result = x1;
                            }
                            else
                            {
                                int length = x1.Length + x2.Length;
                                if (!CheckMaxItemSize(length)) return false;
                                byte[] buffer = new byte[length];
                                Unsafe.MemoryCopy(x1, 0, buffer, 0, x1.Length);
                                Unsafe.MemoryCopy(x2, 0, buffer, x1.Length, x2.Length);
                                result = buffer;
                            }
                            context.EvaluationStack.Push(result);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.SUBSTR:
                        {
                            int count = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (count < 0) return false;
                            int index = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (index < 0) return false;
                            byte[] x = context.EvaluationStack.Pop().GetByteArray();
                            context.EvaluationStack.Push(x.Skip(index).Take(count).ToArray());
                            CheckStackSize(true, -2);
                            break;
                        }
                    case OpCode.LEFT:
                        {
                            int count = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (count < 0) return false;
                            byte[] x = context.EvaluationStack.Pop().GetByteArray();
                            byte[] buffer;
                            if (count >= x.Length)
                            {
                                buffer = x;
                            }
                            else
                            {
                                buffer = new byte[count];
                                Unsafe.MemoryCopy(x, 0, buffer, 0, count);
                            }
                            context.EvaluationStack.Push(buffer);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.RIGHT:
                        {
                            int count = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (count < 0) return false;
                            byte[] x = context.EvaluationStack.Pop().GetByteArray();
                            if (count > x.Length) return false;
                            byte[] buffer;
                            if (count == x.Length)
                            {
                                buffer = x;
                            }
                            else
                            {
                                buffer = new byte[count];
                                Unsafe.MemoryCopy(x, x.Length - count, buffer, 0, count);
                            }
                            context.EvaluationStack.Push(buffer);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.SIZE:
                        {
                            StackItem x = context.EvaluationStack.Pop();
                            context.EvaluationStack.Push(x.GetByteLength());
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
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 & x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.OR:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 | x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.XOR:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
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
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            if (!CheckBigInteger(x)) return false;
                            x += 1;
                            if (!CheckBigInteger(x)) return false;
                            context.EvaluationStack.Push(x);
                            break;
                        }
                    case OpCode.DEC:
                        {
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            if (!CheckBigInteger(x)) return false;
                            x -= 1;
                            if (!CheckBigInteger(x)) return false;
                            context.EvaluationStack.Push(x);
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
                            bool x = context.EvaluationStack.Pop().GetBoolean();
                            context.EvaluationStack.Push(!x);
                            CheckStackSize(false, 0);
                            break;
                        }
                    case OpCode.NZ:
                        {
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(!x.IsZero);
                            break;
                        }
                    case OpCode.ADD:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            if (!CheckBigInteger(x2)) return false;
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            if (!CheckBigInteger(x1)) return false;
                            BigInteger result = x1 + x2;
                            if (!CheckBigInteger(result)) return false;
                            context.EvaluationStack.Push(result);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.SUB:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            if (!CheckBigInteger(x2)) return false;
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            if (!CheckBigInteger(x1)) return false;
                            BigInteger result = x1 - x2;
                            if (!CheckBigInteger(result)) return false;
                            context.EvaluationStack.Push(result);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.MUL:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            if (!CheckBigInteger(x2)) return false;
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            if (!CheckBigInteger(x1)) return false;
                            BigInteger result = x1 * x2;
                            if (!CheckBigInteger(result)) return false;
                            context.EvaluationStack.Push(result);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.DIV:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            if (!CheckBigInteger(x2)) return false;
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            if (!CheckBigInteger(x1)) return false;
                            context.EvaluationStack.Push(x1 / x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.MOD:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            if (!CheckBigInteger(x2)) return false;
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            if (!CheckBigInteger(x1)) return false;
                            context.EvaluationStack.Push(x1 % x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.SHL:
                        {
                            int shift = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (!CheckShift(shift)) return false;
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            if (!CheckBigInteger(x)) return false;
                            x <<= shift;
                            if (!CheckBigInteger(x)) return false;
                            context.EvaluationStack.Push(x);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.SHR:
                        {
                            int shift = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (!CheckShift(shift)) return false;
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            if (!CheckBigInteger(x)) return false;
                            x >>= shift;
                            if (!CheckBigInteger(x)) return false;
                            context.EvaluationStack.Push(x);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.BOOLAND:
                        {
                            bool x2 = context.EvaluationStack.Pop().GetBoolean();
                            bool x1 = context.EvaluationStack.Pop().GetBoolean();
                            context.EvaluationStack.Push(x1 && x2);
                            CheckStackSize(false, -1);
                            break;
                        }
                    case OpCode.BOOLOR:
                        {
                            bool x2 = context.EvaluationStack.Pop().GetBoolean();
                            bool x1 = context.EvaluationStack.Pop().GetBoolean();
                            context.EvaluationStack.Push(x1 || x2);
                            CheckStackSize(false, -1);
                            break;
                        }
                    case OpCode.NUMEQUAL:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 == x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.NUMNOTEQUAL:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 != x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.LT:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 < x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.GT:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 > x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.LTE:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 <= x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.GTE:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(x1 >= x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.MIN:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(BigInteger.Min(x1, x2));
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.MAX:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(BigInteger.Max(x1, x2));
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.WITHIN:
                        {
                            BigInteger b = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger a = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();
                            context.EvaluationStack.Push(a <= x && x < b);
                            CheckStackSize(true, -2);
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
                            CheckStackSize(true, -1);
                            break;
                        }
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
                            CheckStackSize(true, -2);
                            break;
                        }
                    case OpCode.CHECKMULTISIG:
                        {
                            int n;
                            byte[][] pubkeys;
                            StackItem item = context.EvaluationStack.Pop();

                            if (item is VMArray array1)
                            {
                                pubkeys = array1.Select(p => p.GetByteArray()).ToArray();
                                n = pubkeys.Length;
                                if (n == 0) return false;
                                CheckStackSize(false, -1);
                            }
                            else
                            {
                                n = (int)item.GetBigInteger();
                                if (n < 1 || n > context.EvaluationStack.Count) return false;
                                pubkeys = new byte[n][];
                                for (int i = 0; i < n; i++)
                                    pubkeys[i] = context.EvaluationStack.Pop().GetByteArray();
                                CheckStackSize(true, -n - 1);
                            }

                            int m;
                            byte[][] signatures;
                            item = context.EvaluationStack.Pop();
                            if (item is VMArray array2)
                            {
                                signatures = array2.Select(p => p.GetByteArray()).ToArray();
                                m = signatures.Length;
                                if (m == 0 || m > n) return false;
                                CheckStackSize(false, -1);
                            }
                            else
                            {
                                m = (int)item.GetBigInteger();
                                if (m < 1 || m > n || m > context.EvaluationStack.Count) return false;
                                signatures = new byte[m][];
                                for (int i = 0; i < m; i++)
                                    signatures[i] = context.EvaluationStack.Pop().GetByteArray();
                                CheckStackSize(true, -m - 1);
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
                            StackItem item = context.EvaluationStack.Pop();
                            if (item is ICollection collection)
                            {
                                context.EvaluationStack.Push(collection.Count);
                                CheckStackSize(false, 0);
                            }
                            else
                            {
                                context.EvaluationStack.Push(item.GetByteLength());
                                CheckStackSize(true, 0);
                            }
                            break;
                        }
                    case OpCode.PACK:
                        {
                            int size = (int)context.EvaluationStack.Pop().GetBigInteger();
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
                            if (!(context.EvaluationStack.Pop() is VMArray array))
                                return false;
                            for (int i = array.Count - 1; i >= 0; i--)
                                context.EvaluationStack.Push(array[i]);
                            context.EvaluationStack.Push(array.Count);
                            if (!CheckStackSize(false, array.Count)) return false;
                            break;
                        }
                    case OpCode.PICKITEM:
                        {
                            StackItem key = context.EvaluationStack.Pop();
                            if (key is ICollection) return false;
                            StackItem item = context.EvaluationStack.Pop();
                            switch (item)
                            {
                                case VMArray array:
                                    {
                                        int index = (int)key.GetBigInteger();
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
                                default:
                                    {
                                        return false;
                                    }
                            }
                            break;
                        }
                    case OpCode.SETITEM:
                        {
                            StackItem value = context.EvaluationStack.Pop();
                            if (value is Struct s) value = s.Clone();
                            StackItem key = context.EvaluationStack.Pop();
                            if (key is ICollection) return false;
                            switch (context.EvaluationStack.Pop())
                            {
                                case VMArray array:
                                    {
                                        int index = (int)key.GetBigInteger();
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
                            var item = context.EvaluationStack.Pop();

                            if (item is VMArray array)
                            {
                                // Allow to convert between array and struct

                                VMArray result = null;

                                if (array is Struct)
                                {
                                    if (instruction.OpCode == OpCode.NEWSTRUCT)
                                        result = array;
                                }
                                else
                                {
                                    if (instruction.OpCode == OpCode.NEWARRAY)
                                        result = array;
                                }

                                if (result is null)
                                    result = instruction.OpCode == OpCode.NEWARRAY
                                        ? new VMArray(array)
                                        : new Struct(array);

                                context.EvaluationStack.Push(result);
                                if (!CheckStackSize(false, int.MaxValue)) return false;
                            }
                            else
                            {
                                int count = (int)item.GetBigInteger();

                                if (count < 0 || !CheckArraySize(count)) return false;

                                List<StackItem> items = new List<StackItem>(count);

                                for (var i = 0; i < count; i++)
                                {
                                    items.Add(false);
                                }

                                VMArray result = instruction.OpCode == OpCode.NEWARRAY
                                    ? new VMArray(items)
                                    : new Struct(items);

                                context.EvaluationStack.Push(result);
                                if (!CheckStackSize(true, count)) return false;
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
                            StackItem key = context.EvaluationStack.Pop();
                            if (key is ICollection) return false;
                            StackItem value = context.EvaluationStack.Pop();
                            CheckStackSize(false, -2);
                            switch (value)
                            {
                                case VMArray array:
                                    int index = (int)key.GetBigInteger();
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
                    case OpCode.HASKEY:
                        {
                            StackItem key = context.EvaluationStack.Pop();
                            if (key is ICollection) return false;
                            switch (context.EvaluationStack.Pop())
                            {
                                case VMArray array:
                                    int index = (int)key.GetBigInteger();
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
                            if (!(context.EvaluationStack.Pop() is Map map)) return false;
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

                    // Stack isolation
                    case OpCode.CALL_I:
                        {
                            if (!CheckMaxInvocationStack()) return false;
                            int rvcount = instruction.Operand[0];
                            int pcount = instruction.Operand[1];
                            if (context.EvaluationStack.Count < pcount) return false;
                            ExecutionContext context_call = LoadScript(context.Script, rvcount);
                            context_call.InstructionPointer = context.InstructionPointer + instruction.TokenI16_1 + 2;
                            if (context_call.InstructionPointer < 0 || context_call.InstructionPointer > context_call.Script.Length) return false;
                            context.EvaluationStack.CopyTo(context_call.EvaluationStack, pcount);
                            for (int i = 0; i < pcount; i++)
                                context.EvaluationStack.Pop();
                            break;
                        }
                    case OpCode.CALL_E:
                    case OpCode.CALL_ED:
                    case OpCode.CALL_ET:
                    case OpCode.CALL_EDT:
                        {
                            if (table == null) return false;
                            int rvcount = instruction.Operand[0];
                            int pcount = instruction.Operand[1];
                            if (context.EvaluationStack.Count < pcount) return false;
                            if (instruction.OpCode == OpCode.CALL_ET || instruction.OpCode == OpCode.CALL_EDT)
                            {
                                if (context.RVCount != rvcount) return false;
                            }
                            else
                            {
                                if (!CheckMaxInvocationStack()) return false;
                            }

                            byte[] script_hash;
                            if (instruction.OpCode == OpCode.CALL_ED || instruction.OpCode == OpCode.CALL_EDT)
                            {
                                script_hash = context.EvaluationStack.Pop().GetByteArray();
                                CheckStackSize(true, -1);
                            }
                            else
                            {
                                script_hash = instruction.ReadBytes(2, 20);
                            }

                            ExecutionContext context_new = LoadScriptByHash(script_hash, rvcount);
                            if (context_new == null) return false;
                            context.EvaluationStack.CopyTo(context_new.EvaluationStack, pcount);
                            if (instruction.OpCode == OpCode.CALL_ET || instruction.OpCode == OpCode.CALL_EDT)
                                InvocationStack.Remove(1);
                            else
                                for (int i = 0; i < pcount; i++)
                                    context.EvaluationStack.Pop();
                            break;
                        }

                    // Exceptions
                    case OpCode.THROW:
                        {
                            return false;
                        }
                    case OpCode.THROWIFNOT:
                        {
                            if (!context.EvaluationStack.Pop().GetBoolean())
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

        public ExecutionContext LoadScript(byte[] script, int rvcount = -1)
        {
            return LoadScript(new Script(Crypto, script), rvcount);
        }

        protected virtual ExecutionContext LoadScript(Script script, int rvcount = -1)
        {
            ExecutionContext context = new ExecutionContext(script, rvcount);
            InvocationStack.Push(context);
            return context;
        }

        private ExecutionContext LoadScriptByHash(byte[] hash, int rvcount = -1)
        {
            if (table == null) return null;
            byte[] script = table.GetScript(hash);
            if (script == null) return null;
            return LoadScript(new Script(hash, script), rvcount);
        }

        protected virtual bool PostExecuteInstruction(Instruction instruction)
        {
            return true;
        }

        protected virtual bool PreExecuteInstruction()
        {
            return true;
        }
    }
}
