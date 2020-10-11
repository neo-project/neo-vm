using System;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    public sealed class ExecutionEngineLimmits
    {
        /// <summary>
        /// Default limits
        /// </summary>
        public static readonly ExecutionEngineLimmits Default = new ExecutionEngineLimmits();

        /// <summary>
        /// Max value for SHL and SHR
        /// </summary>
        public int MaxShift { get; private set; } = 256;

        /// <summary>
        /// Set the max Stack Size
        /// </summary>
        public uint MaxStackSize { get; private set; } = 2 * 1024;

        /// <summary>
        /// Set Max Item Size
        /// </summary>
        public uint MaxItemSize { get; private set; } = 1024 * 1024;

        /// <summary>
        /// Set Max Invocation Stack Size
        /// </summary>
        public uint MaxInvocationStackSize { get; private set; } = 1024;

        public uint MaxTryNestingDepth { get; private set; } = 16;

        /// <summary>
        /// Constructor
        /// </summary>
        public ExecutionEngineLimmits() { }

        /// <summary>
        /// Constructor
        /// </summary>
        public ExecutionEngineLimmits(int maxShift, uint maxStackSize, uint maxItemSize, uint maxInvocationStackSize, uint maxTryNestingDepth)
        {
            MaxShift = maxShift;
            MaxStackSize = maxStackSize;
            MaxItemSize = maxItemSize;
            MaxInvocationStackSize = maxInvocationStackSize;
            MaxTryNestingDepth = maxTryNestingDepth;
        }

        /// <summary>
        /// Check if the is possible to overflow the MaxItemSize
        /// </summary>
        /// <param name="length">Length</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AssertMaxItemSize(int length)
        {
            if (length < 0 || length > MaxItemSize)
            {
                throw new InvalidOperationException($"MaxItemSize exceed: {length}");
            }
        }

        /// <summary>
        /// Check if the number is allowed from SHL and SHR
        /// </summary>
        /// <param name="shift">Shift</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AssertShift(int shift)
        {
            if (shift > MaxShift || shift < 0)
            {
                throw new InvalidOperationException($"Invalid shift value: {shift}");
            }
        }
    }
}
