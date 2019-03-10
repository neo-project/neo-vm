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
        public virtual int Max_SHL_SHR => ushort.MaxValue;

        /// <summary>
        /// Min value for SHL and SHR
        /// </summary>
        public virtual int Min_SHL_SHR => -ushort.MaxValue;

        /// <summary>
        /// Set the max size in bytes allowed size for BigInteger
        /// </summary>
        public virtual int MaxSizeForBigInteger => 32;

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

        private readonly IScriptTable table;
        private readonly Dictionary<byte[], HashSet<uint>> break_points = new Dictionary<byte[], HashSet<uint>>(new HashComparer());

        public IScriptContainer ScriptContainer { get; }
        public ICrypto Crypto { get; }
        public IInteropService Service { get; }
        public RandomAccessStack<ExecutionContext> InvocationStack { get; } = new RandomAccessStack<ExecutionContext>();
        public RandomAccessStack<StackItem> ResultStack { get; } = new RandomAccessStack<StackItem>();
        public ExecutionContext CurrentContext => InvocationStack.Peek();
        public ExecutionContext CallingContext => InvocationStack.Count > 1 ? InvocationStack.Peek(1) : null;
        public ExecutionContext EntryContext => InvocationStack.Peek(InvocationStack.Count - 1);
        public VMState State { get; protected set; } = VMState.BREAK;

        public ExecutionEngine(IScriptContainer container, ICrypto crypto, IScriptTable table = null, IInteropService service = null)
        {
            this.ScriptContainer = container;
            this.Crypto = crypto;
            this.table = table;
            this.Service = service;
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
        /// Check if the BigInteger is allowed for numeric operations
        /// </summary>
        /// <param name="byteLength">Value</param>
        /// <returns>Return True if are allowed, otherwise False</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckBigIntegerByteLength(int byteLength) => byteLength <= MaxSizeForBigInteger;

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
            while (InvocationStack.Count > 0)
                InvocationStack.Pop().Dispose();
        }

        public void Execute()
        {
            State &= ~VMState.BREAK;
            while (!State.HasFlag(VMState.HALT) && !State.HasFlag(VMState.FAULT) && !State.HasFlag(VMState.BREAK))
                ExecuteNext();
        }

        protected void ExecuteNext()
        {
            if (InvocationStack.Count == 0)
            {
                State = VMState.HALT;
            }
            else
            {
                OpCode opcode = CurrentContext.InstructionPointer >= CurrentContext.Script.Length ? OpCode.RET : (OpCode)CurrentContext.OpReader.ReadByte();
                try
                {
                    ExecuteOp(opcode, CurrentContext);
                }
                catch
                {
                    State = VMState.FAULT;
                }
                if (State == VMState.NONE && InvocationStack.Count > 0)
                {
                    if (break_points.TryGetValue(CurrentContext.ScriptHash, out HashSet<uint> hashset) && hashset.Contains((uint)CurrentContext.InstructionPointer))
                        State = VMState.BREAK;
                }
            }
        }

        private void ExecuteOp(OpCode opcode, ExecutionContext context)
        {
            if (opcode >= OpCode.PUSHBYTES1 && opcode <= OpCode.PUSHBYTES75)
            {
                context.EvaluationStack.Push(context.OpReader.SafeReadBytes((byte)opcode));

                if (!CheckStackSize(true))
                {
                    State = VMState.FAULT;
                    return;
                }
            }
            else switch (opcode)
                {
                    // Push value
                    case OpCode.PUSH0:
                        {
                            context.EvaluationStack.Push(new byte[0]);

                            if (!CheckStackSize(true))
                            {
                                State = VMState.FAULT;
                                return;
                            }
                            break;
                        }
                    case OpCode.PUSHDATA1:
                        {
                            context.EvaluationStack.Push(context.OpReader.SafeReadBytes(context.OpReader.ReadByte()));

                            if (!CheckStackSize(true))
                            {
                                State = VMState.FAULT;
                                return;
                            }
                            break;
                        }
                    case OpCode.PUSHDATA2:
                        {
                            context.EvaluationStack.Push(context.OpReader.SafeReadBytes(context.OpReader.ReadUInt16()));

                            if (!CheckStackSize(true))
                            {
                                State = VMState.FAULT;
                                return;
                            }
                            break;
                        }
                    case OpCode.PUSHDATA4:
                        {
                            int length = context.OpReader.ReadInt32();

                            if (!CheckMaxItemSize(length))
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(context.OpReader.SafeReadBytes(length));

                            if (!CheckStackSize(true))
                            {
                                State = VMState.FAULT;
                                return;
                            }
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
                            context.EvaluationStack.Push((int)opcode - (int)OpCode.PUSH1 + 1);

                            if (!CheckStackSize(true))
                            {
                                State = VMState.FAULT;
                                return;
                            }
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
                                State = VMState.FAULT;
                                return;
                            }
                            bool fValue = true;
                            if (opcode > OpCode.JMP)
                            {
                                CheckStackSize(false, -1);
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
                            if (!CheckMaxInvocationStack())
                            {
                                State = VMState.FAULT;
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
                            using (ExecutionContext context_pop = InvocationStack.Pop())
                            {
                                int rvcount = context_pop.RVCount;
                                if (rvcount == -1) rvcount = context_pop.EvaluationStack.Count;
                                if (rvcount > 0)
                                {
                                    if (context_pop.EvaluationStack.Count < rvcount)
                                    {
                                        State = VMState.FAULT;
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

                            CheckStackSize(false, 0);

                            if (InvocationStack.Count == 0)
                            {
                                State = VMState.HALT;
                            }

                            break;
                        }
                    case OpCode.APPCALL:
                    case OpCode.TAILCALL:
                        {
                            if (table == null || (opcode == OpCode.APPCALL && !CheckMaxInvocationStack()))
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            byte[] script_hash = context.OpReader.SafeReadBytes(20);
                            if (script_hash.All(p => p == 0))
                            {
                                script_hash = context.EvaluationStack.Pop().GetByteArray();
                            }

                            ExecutionContext context_new = LoadScriptByHash(script_hash);
                            if (context_new == null)
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.CopyTo(context_new.EvaluationStack);
                            if (opcode == OpCode.TAILCALL)
                                InvocationStack.Remove(1).Dispose();
                            else
                                context.EvaluationStack.Clear();

                            CheckStackSize(false, 0);

                            break;
                        }
                    case OpCode.SYSCALL:
                        {
                            if (Service?.Invoke(context.OpReader.ReadVarBytes(252), this) != true || !CheckStackSize(false, int.MaxValue))
                            {
                                State = VMState.FAULT;
                                return;
                            }
                            break;
                        }

                    // Stack ops
                    case OpCode.DUPFROMALTSTACK:
                        {
                            context.EvaluationStack.Push(context.AltStack.Peek());

                            if (!CheckStackSize(true))
                            {
                                State = VMState.FAULT;
                                return;
                            }
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
                            if (n < 0)
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Remove(n);
                            CheckStackSize(false, -2);
                            break;
                        }
                    case OpCode.XSWAP:
                        {
                            int n = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (n < 0)
                            {
                                State = VMState.FAULT;
                                return;
                            }

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
                            if (n <= 0)
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Insert(n, context.EvaluationStack.Peek());
                            break;
                        }
                    case OpCode.DEPTH:
                        {
                            context.EvaluationStack.Push(context.EvaluationStack.Count);

                            if (!CheckStackSize(true))
                            {
                                State = VMState.FAULT;
                                return;
                            }
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

                            if (!CheckStackSize(true))
                            {
                                State = VMState.FAULT;
                                return;
                            }
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

                            if (!CheckStackSize(true))
                            {
                                State = VMState.FAULT;
                                return;
                            }
                            break;
                        }
                    case OpCode.PICK:
                        {
                            int n = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (n < 0)
                            {
                                State = VMState.FAULT;
                                return;
                            }
                            context.EvaluationStack.Push(context.EvaluationStack.Peek(n));
                            break;
                        }
                    case OpCode.ROLL:
                        {
                            int n = (int)context.EvaluationStack.Pop().GetBigInteger();

                            if (n < 0)
                            {
                                State = VMState.FAULT;
                                return;
                            }

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

                            if (!CheckStackSize(true))
                            {
                                State = VMState.FAULT;
                                return;
                            }
                            break;
                        }
                    case OpCode.CAT:
                        {
                            byte[] x2 = context.EvaluationStack.Pop().GetByteArray();
                            byte[] x1 = context.EvaluationStack.Pop().GetByteArray();

                            if (!CheckMaxItemSize(x1.Length + x2.Length))
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(x1.Concat(x2).ToArray());
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.SUBSTR:
                        {
                            int count = (int)context.EvaluationStack.Pop().GetBigInteger();

                            if (count < 0)
                            {
                                State = VMState.FAULT;
                                return;
                            }
                            int index = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (index < 0)
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            byte[] x = context.EvaluationStack.Pop().GetByteArray();
                            context.EvaluationStack.Push(x.Skip(index).Take(count).ToArray());
                            CheckStackSize(true, -2);
                            break;
                        }
                    case OpCode.LEFT:
                        {
                            int count = (int)context.EvaluationStack.Pop().GetBigInteger();

                            if (count < 0)
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            byte[] x = context.EvaluationStack.Pop().GetByteArray();
                            context.EvaluationStack.Push(x.Take(count).ToArray());
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.RIGHT:
                        {
                            int count = (int)context.EvaluationStack.Pop().GetBigInteger();

                            if (count < 0)
                            {
                                State = VMState.FAULT;
                                return;
                            }
                            byte[] x = context.EvaluationStack.Pop().GetByteArray();
                            if (x.Length < count)
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(x.Skip(x.Length - count).ToArray());
                            CheckStackSize(true, -1);
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

                            if (!CheckBigInteger(x) || !CheckBigInteger(x + 1))
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(x + 1);
                            break;
                        }
                    case OpCode.DEC:
                        {
                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();

                            if (!CheckBigInteger(x) || (x.Sign <= 0 && !CheckBigInteger(x - 1)))
                            {
                                State = VMState.FAULT;
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
                            bool x = context.EvaluationStack.Pop().GetBoolean();
                            context.EvaluationStack.Push(!x);
                            CheckStackSize(false, 0);
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
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();

                            if (!CheckBigInteger(x2) || !CheckBigInteger(x1) || !CheckBigInteger(x1 + x2))
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(x1 + x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.SUB:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();

                            if (!CheckBigInteger(x2) || !CheckBigInteger(x1) || !CheckBigInteger(x1 - x2))
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(x1 - x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.MUL:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();

                            int lx1 = x1.ToByteArray().Length;

                            if (!CheckBigIntegerByteLength(lx1))
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            int lx2 = x2.ToByteArray().Length;

                            if (!CheckBigIntegerByteLength(lx1 + lx2))
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(x1 * x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.DIV:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();

                            if (!CheckBigInteger(x2) || !CheckBigInteger(x1))
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(x1 / x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.MOD:
                        {
                            BigInteger x2 = context.EvaluationStack.Pop().GetBigInteger();
                            BigInteger x1 = context.EvaluationStack.Pop().GetBigInteger();

                            if (!CheckBigInteger(x2) || !CheckBigInteger(x1))
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(x1 % x2);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.SHL:
                        {
                            int shift = (int)context.EvaluationStack.Pop().GetBigInteger();

                            if (!CheckShift(shift))
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();

                            if (!CheckBigInteger(x))
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            x = x << shift;

                            if (!CheckBigInteger(x))
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(x);
                            CheckStackSize(true, -1);
                            break;
                        }
                    case OpCode.SHR:
                        {
                            int shift = (int)context.EvaluationStack.Pop().GetBigInteger();

                            if (!CheckShift(shift))
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            BigInteger x = context.EvaluationStack.Pop().GetBigInteger();

                            if (!CheckBigInteger(x))
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            context.EvaluationStack.Push(x >> shift);
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
                                if (n == 0)
                                {
                                    State = VMState.FAULT;
                                    return;
                                }
                                CheckStackSize(false, -1);
                            }
                            else
                            {
                                n = (int)item.GetBigInteger();
                                if (n < 1 || n > context.EvaluationStack.Count)
                                {
                                    State = VMState.FAULT;
                                    return;
                                }
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
                                if (m == 0 || m > n)
                                {
                                    State = VMState.FAULT;
                                    return;
                                }
                                CheckStackSize(false, -1);
                            }
                            else
                            {
                                m = (int)item.GetBigInteger();
                                if (m < 1 || m > n || m > context.EvaluationStack.Count)
                                {
                                    State = VMState.FAULT;
                                    return;
                                }
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
                                context.EvaluationStack.Push(item.GetByteArray().Length);
                                CheckStackSize(true, 0);
                            }
                            break;
                        }
                    case OpCode.PACK:
                        {
                            int size = (int)context.EvaluationStack.Pop().GetBigInteger();
                            if (size < 0 || size > context.EvaluationStack.Count || !CheckArraySize(size))
                            {
                                State = VMState.FAULT;
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
                                for (int i = array.Count - 1; i >= 0; i--)
                                    context.EvaluationStack.Push(array[i]);

                                context.EvaluationStack.Push(array.Count);

                                if (!CheckStackSize(false, array.Count))
                                {
                                    State = VMState.FAULT;
                                    return;
                                }
                            }
                            else
                            {
                                State = VMState.FAULT;
                                return;
                            }
                            break;
                        }
                    case OpCode.PICKITEM:
                        {
                            StackItem key = context.EvaluationStack.Pop();
                            if (key is ICollection)
                            {
                                State = VMState.FAULT;
                                return;
                            }
                            switch (context.EvaluationStack.Pop())
                            {
                                case VMArray array:
                                    int index = (int)key.GetBigInteger();
                                    if (index < 0 || index >= array.Count)
                                    {
                                        State = VMState.FAULT;
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
                                        State = VMState.FAULT;
                                        return;
                                    }
                                    break;
                                default:
                                    State = VMState.FAULT;
                                    return;
                            }

                            if (!CheckStackSize(false, int.MaxValue))
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            break;
                        }
                    case OpCode.SETITEM:
                        {
                            StackItem value = context.EvaluationStack.Pop();
                            if (value is Struct s) value = s.Clone();
                            StackItem key = context.EvaluationStack.Pop();
                            if (key is ICollection)
                            {
                                State = VMState.FAULT;
                                return;
                            }
                            switch (context.EvaluationStack.Pop())
                            {
                                case VMArray array:
                                    {
                                        int index = (int)key.GetBigInteger();
                                        if (index < 0 || index >= array.Count)
                                        {
                                            State = VMState.FAULT;
                                            return;
                                        }
                                        array[index] = value;
                                        break;
                                    }
                                case Map map:
                                    {
                                        if (!map.ContainsKey(key) && !CheckArraySize(map.Count + 1))
                                        {
                                            State = VMState.FAULT;
                                            return;
                                        }

                                        map[key] = value;
                                        break;
                                    }
                                default:
                                    {
                                        State = VMState.FAULT;
                                        return;
                                    }
                            }

                            if (!CheckStackSize(false, int.MaxValue))
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            break;
                        }
                    case OpCode.NEWARRAY:
                        {
                            int count = (int)context.EvaluationStack.Pop().GetBigInteger();

                            if (!CheckArraySize(count))
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            List<StackItem> items = new List<StackItem>(count);
                            for (var i = 0; i < count; i++)
                            {
                                items.Add(false);
                            }

                            context.EvaluationStack.Push(new Types.Array(items));

                            if (!CheckStackSize(true, count))
                            {
                                State = VMState.FAULT;
                                return;
                            }
                        }
                        break;
                    case OpCode.NEWSTRUCT:
                        {
                            int count = (int)context.EvaluationStack.Pop().GetBigInteger();

                            if (!CheckArraySize(count))
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            List<StackItem> items = new List<StackItem>(count);
                            for (var i = 0; i < count; i++)
                            {
                                items.Add(false);
                            }
                            context.EvaluationStack.Push(new VM.Types.Struct(items));

                            if (!CheckStackSize(true, count))
                            {
                                State = VMState.FAULT;
                                return;
                            }
                            break;
                        }
                    case OpCode.NEWMAP:
                        {
                            context.EvaluationStack.Push(new Map());

                            if (!CheckStackSize(true))
                            {
                                State = VMState.FAULT;
                                return;
                            }
                            break;
                        }
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
                                if (!CheckArraySize(array.Count + 1))
                                {
                                    State = VMState.FAULT;
                                    return;
                                }

                                array.Add(newItem);
                            }
                            else
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            if (!CheckStackSize(false, int.MaxValue))
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            break;
                        }
                    case OpCode.REVERSE:
                        {
                            StackItem arrItem = context.EvaluationStack.Pop();
                            CheckStackSize(false, -1);

                            if (arrItem is VMArray array)
                            {
                                array.Reverse();
                            }
                            else
                            {
                                State = VMState.FAULT;
                                return;
                            }
                            break;
                        }
                    case OpCode.REMOVE:
                        {
                            StackItem key = context.EvaluationStack.Pop();
                            if (key is ICollection)
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            StackItem value = context.EvaluationStack.Pop();
                            CheckStackSize(false, -2);

                            switch (value)
                            {
                                case VMArray array:
                                    int index = (int)key.GetBigInteger();
                                    if (index < 0 || index >= array.Count)
                                    {
                                        State = VMState.FAULT;
                                        return;
                                    }
                                    array.RemoveAt(index);
                                    break;
                                case Map map:
                                    map.Remove(key);
                                    break;
                                default:
                                    State = VMState.FAULT;
                                    return;
                            }
                            break;
                        }
                    case OpCode.HASKEY:
                        {
                            StackItem key = context.EvaluationStack.Pop();
                            if (key is ICollection)
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            switch (context.EvaluationStack.Pop())
                            {
                                case VMArray array:
                                    int index = (int)key.GetBigInteger();
                                    if (index < 0)
                                    {
                                        State = VMState.FAULT;
                                        return;
                                    }
                                    context.EvaluationStack.Push(index < array.Count);
                                    break;
                                case Map map:
                                    context.EvaluationStack.Push(map.ContainsKey(key));
                                    break;
                                default:
                                    State = VMState.FAULT;
                                    return;
                            }

                            CheckStackSize(false, -1);
                            break;
                        }
                    case OpCode.KEYS:
                        {
                            switch (context.EvaluationStack.Pop())
                            {
                                case Map map:
                                    {
                                        context.EvaluationStack.Push(new VMArray(map.Keys));

                                        if (!CheckStackSize(false, map.Count))
                                        {
                                            State = VMState.FAULT;
                                            return;
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        State = VMState.FAULT;
                                        return;
                                    }
                            }
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
                                    State = VMState.FAULT;
                                    return;
                            }
                            List<StackItem> newArray = new List<StackItem>(values.Count);
                            foreach (StackItem item in values)
                                if (item is Struct s)
                                    newArray.Add(s.Clone());
                                else
                                    newArray.Add(item);
                            context.EvaluationStack.Push(new VMArray(newArray));

                            if (!CheckStackSize(false, int.MaxValue))
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            break;
                        }

                    // Stack isolation
                    case OpCode.CALL_I:
                        {
                            if (!CheckMaxInvocationStack())
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            int rvcount = context.OpReader.ReadByte();
                            int pcount = context.OpReader.ReadByte();
                            if (context.EvaluationStack.Count < pcount)
                            {
                                State = VMState.FAULT;
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
                                State = VMState.FAULT;
                                return;
                            }

                            int rvcount = context.OpReader.ReadByte();
                            int pcount = context.OpReader.ReadByte();

                            if (context.EvaluationStack.Count < pcount)
                            {
                                State = VMState.FAULT;
                                return;
                            }

                            if (opcode == OpCode.CALL_ET || opcode == OpCode.CALL_EDT)
                            {
                                if (context.RVCount != rvcount)
                                {
                                    State = VMState.FAULT;
                                    return;
                                }
                            }
                            else
                            {
                                if (!CheckMaxInvocationStack())
                                {
                                    State = VMState.FAULT;
                                    return;
                                }
                            }

                            byte[] script_hash;
                            if (opcode == OpCode.CALL_ED || opcode == OpCode.CALL_EDT)
                            {
                                script_hash = context.EvaluationStack.Pop().GetByteArray();
                                CheckStackSize(true, -1);
                            }
                            else
                            {
                                script_hash = context.OpReader.SafeReadBytes(20);
                            }

                            ExecutionContext context_new = LoadScriptByHash(script_hash, rvcount);
                            if (context_new == null)
                            {
                                State = VMState.FAULT;
                                return;
                            }
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
                            State = VMState.FAULT;
                            return;
                        }
                    case OpCode.THROWIFNOT:
                        {
                            if (!context.EvaluationStack.Pop().GetBoolean())
                            {
                                State = VMState.FAULT;
                                return;
                            }
                            CheckStackSize(false, -1);
                            break;
                        }
                    default:
                        {
                            State = VMState.FAULT;
                            return;
                        }
                }
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
            if (State.HasFlag(VMState.HALT) || State.HasFlag(VMState.FAULT)) return;
            ExecuteNext();
            if (State == VMState.NONE)
                State = VMState.BREAK;
        }

        public void StepOut()
        {
            State &= ~VMState.BREAK;
            int c = InvocationStack.Count;
            while (!State.HasFlag(VMState.HALT) && !State.HasFlag(VMState.FAULT) && !State.HasFlag(VMState.BREAK) && InvocationStack.Count >= c)
                ExecuteNext();
            if (State == VMState.NONE)
                State = VMState.BREAK;
        }

        public void StepOver()
        {
            if (State.HasFlag(VMState.HALT) || State.HasFlag(VMState.FAULT)) return;
            State &= ~VMState.BREAK;
            int c = InvocationStack.Count;
            do
            {
                ExecuteNext();
            } while (!State.HasFlag(VMState.HALT) && !State.HasFlag(VMState.FAULT) && !State.HasFlag(VMState.BREAK) && InvocationStack.Count > c);
            if (State == VMState.NONE)
                State = VMState.BREAK;
        }
    }
}
