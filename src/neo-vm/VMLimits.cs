using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Neo.VM.Types;

namespace Neo.VM
{
    public class VMLimits
    {
        /// <summary>
        /// Default limits
        /// </summary>
        public static VMLimits Default { get; set; } = new VMLimits();

        /// <summary>
        /// Max value for SHL and SHR
        /// </summary>
        public int Max_SHL_SHR = ushort.MaxValue;

        /// <summary>
        /// Min value for SHL and SHR
        /// </summary>
        public int Min_SHL_SHR = -ushort.MaxValue;

        /// <summary>
        /// Set the max size allowed size for BigInteger
        /// </summary>
        public int MaxSizeForBigInteger = 32;

        /// <summary>
        /// Set the max Stack Size
        /// </summary>
        public uint MaxStackSize = 2 * 1024;

        /// <summary>
        /// Set Max Item Size
        /// </summary>
        public uint MaxItemSize = 1024 * 1024;

        /// <summary>
        /// Set Max Invocation Stack Size
        /// </summary>
        public uint MaxInvocationStackSize = 1024;

        /// <summary>
        /// Set Max Array Size
        /// </summary>
        public uint MaxArraySize = 1024;

        /// <summary>
        /// Check if the is possible to overflow the MaxArraySize
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
        public bool CheckMaxItemSize(int length) => length <= MaxItemSize;

        /// <summary>
        /// Check if the is possible to overflow the MaxInvocationStack
        /// </summary>
        /// <param name="stack">Stack</param>
        /// <returns>Return True if are allowed, otherwise False</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckMaxInvocationStack(ExecutionEngine stack) => stack.InvocationStack.Count < MaxInvocationStackSize;

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
        public bool CheckBigIntegerBitLength(int byteLength) => byteLength <= MaxSizeForBigInteger;

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
        /// <param name="engine">Engine</param>
        /// <param name="stackitem_count">Stack item count</param>
        /// <param name="is_stackitem_count_strict">Is stack count strict?</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckStackSize(ExecutionEngine engine, bool is_stackitem_count_strict, int stackitem_count = 1)
        {
            engine.is_stackitem_count_strict = is_stackitem_count_strict;

            return CheckStackSize(engine, stackitem_count);
        }

        /// <summary>
        /// Check if the is possible to overflow the MaxStackSize
        /// </summary>
        /// <param name="engine">Engine</param>
        /// <param name="stackitem_count">Stack item count</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckStackSize(ExecutionEngine engine, int stackitem_count = 1)
        {
            engine.stackitem_count += stackitem_count;

            if (engine.stackitem_count <= MaxStackSize) return true;
            if (engine.is_stackitem_count_strict) return false;
            engine.stackitem_count = GetItemCount(engine.InvocationStack.SelectMany(p => p.EvaluationStack.Concat(p.AltStack)));
            if (engine.stackitem_count > MaxStackSize) return false;
            engine.is_stackitem_count_strict = true;

            return true;
        }

        /// <summary>
        /// Decrease stack item count
        /// </summary>
        /// <param name="engine">Engine</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DecreaseStackItem(ExecutionEngine engine, int count = 1)
        {
            engine.stackitem_count -= count;
        }

        /// <summary>
        /// Decrease stack item count without strict
        /// </summary>
        /// <param name="engine">Engine</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DecreaseStackItemWithoutStrict(ExecutionEngine engine, int count = 1)
        {
            engine.stackitem_count -= count;
            engine.is_stackitem_count_strict = false;
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
    }
}