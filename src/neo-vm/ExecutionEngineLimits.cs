using System;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    public sealed class ExecutionEngineLimits
    {
        /// <summary>
        /// Default limits
        /// </summary>
        public static readonly ExecutionEngineLimits Default = new ExecutionEngineLimits();

        /// <summary>
        /// Max value for SHL and SHR
        /// </summary>
        public int MaxShift { get; } = 256;

        /// <summary>
        /// Set the max Stack Size
        /// </summary>
        public uint MaxStackSize { get; } = 2 * 1024;

        /// <summary>
        /// Set Max Item Size
        /// </summary>
        public uint MaxItemSize { get; } = 1024 * 1024;

        /// <summary>
        /// Set Max Invocation Stack Size
        /// </summary>
        public uint MaxInvocationStackSize { get; } = 1024;

        /// <summary>
        /// Set Max TryStack Count
        /// </summary>
        public uint MaxTryNestingDepth { get; } = 16;

        /// <summary>
        /// Constructor
        /// </summary>
        private ExecutionEngineLimits() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="maxShift">Max value for SHL and SHR</param>
        /// <param name="maxStackSize">Set the max Stack Size</param>
        /// <param name="maxItemSize">Set Max Item Size</param>
        /// <param name="maxInvocationStackSize">Set Max Invocation Stack Size</param>
        /// <param name="maxTryNestingDepth">Set Max TryStack Count</param>
        public ExecutionEngineLimits(int maxShift, uint maxStackSize, uint maxItemSize, uint maxInvocationStackSize, uint maxTryNestingDepth)
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
