// Copyright (C) 2016-2022 The Neo Project.
//
// The neo-vm is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    /// <summary>
    /// Represents the restrictions on the VM.
    /// </summary>
    public sealed class ExecutionEngineLimits : IEquatable<ExecutionEngineLimits>
    {
        /// <summary>
        /// The default strategy.
        /// </summary>
        public static readonly ExecutionEngineLimits Default = new ExecutionEngineLimits();

        /// <summary>
        /// The maximum number of bits that <see cref="OpCode.SHL"/> and <see cref="OpCode.SHR"/> can shift.
        /// </summary>
        public int MaxShift
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = 256;

        /// <summary>
        /// The maximum number of items that can be contained in the VM's evaluation stacks and slots.
        /// </summary>
        public uint MaxStackSize
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = 2 * 1024;

        /// <summary>
        /// The maximum size of an item in the VM.
        /// </summary>
        public uint MaxItemSize
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = 1024 * 1024;

        /// <summary>
        /// The largest comparable size. If a <see cref="Types.ByteString"/> or <see cref="Types.Struct"/> exceeds this size, comparison operations on it cannot be performed in the VM.
        /// </summary>
        public uint MaxComparableSize
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = 65536;

        /// <summary>
        /// The maximum number of frames in the invocation stack of the VM.
        /// </summary>
        public uint MaxInvocationStackSize
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = 1024;

        /// <summary>
        /// The maximum nesting depth of <see langword="try"/>-<see langword="catch"/>-<see langword="finally"/> blocks.
        /// </summary>
        public uint MaxTryNestingDepth
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = 16;

        /// <summary>
        /// Allow to catch the ExecutionEngine Exceptions
        /// </summary>
        public bool CatchEngineExceptions
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = true;

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

        public bool Equals(ExecutionEngineLimits? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            return MaxShift == other.MaxShift
                && MaxStackSize == other.MaxStackSize
                && MaxItemSize == other.MaxItemSize
                && MaxComparableSize == other.MaxComparableSize
                && MaxInvocationStackSize == other.MaxInvocationStackSize
                && MaxInvocationStackSize == other.MaxInvocationStackSize
                && MaxTryNestingDepth == other.MaxTryNestingDepth
                && CatchEngineExceptions == other.CatchEngineExceptions;
        }
    }
}
