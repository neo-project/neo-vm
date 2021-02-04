using System;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    /// <summary>
    /// Represents the restrictions on the VM.
    /// </summary>
    public sealed class ExecutionEngineLimits
    {
        /// <summary>
        /// The default strategy.
        /// </summary>
        public static readonly ExecutionEngineLimits Default = new ExecutionEngineLimits();

        /// <summary>
        /// The maximum number of bits that <see cref="OpCode.SHL"/> and <see cref="OpCode.SHR"/> can shift.
        /// </summary>
        public int MaxShift { get; init; } = 256;

        /// <summary>
        /// The maximum number of items that can be contained in the VM's evaluation stacks and slots.
        /// </summary>
        public uint MaxStackSize { get; init; } = 2 * 1024;

        /// <summary>
        /// The maximum size of an item in the VM.
        /// </summary>
        public uint MaxItemSize { get; init; } = 1024 * 1024;

        /// <summary>
        /// The maximum number of frames in the invocation stack of the VM.
        /// </summary>
        public uint MaxInvocationStackSize { get; init; } = 1024;

        /// <summary>
        /// The maximum nesting depth of <see langword="try"/>-<see langword="catch"/>-<see langword="finally"/> blocks.
        /// </summary>
        public uint MaxTryNestingDepth { get; init; } = 16;

        /// <summary>
        /// Assert that the size of the item meets the limit.
        /// </summary>
        /// <param name="size">The size to be checked.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AssertMaxItemSize(int size)
        {
            if (size < 0 || size > MaxItemSize)
            {
                throw new InvalidOperationException($"MaxItemSize exceed: {size}");
            }
        }

        /// <summary>
        /// Assert that the number of bits shifted meets the limit.
        /// </summary>
        /// <param name="shift">The number of bits shifted.</param>
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
