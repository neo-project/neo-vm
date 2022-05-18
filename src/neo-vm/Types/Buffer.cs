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
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Neo.VM.Types
{
    /// <summary>
    /// Represents a memory block that can be used for reading and writing in the VM.
    /// </summary>
    [DebuggerDisplay("Type={GetType().Name}, Value={System.BitConverter.ToString(InnerBuffer).Replace(\"-\", string.Empty)}")]
    public class Buffer : StackItem
    {
        /// <summary>
        /// The internal byte array used to store the actual data.
        /// </summary>
        public readonly byte[] InnerBuffer;

        /// <summary>
        /// The size of the buffer.
        /// </summary>
        public int Size => InnerBuffer.Length;
        public override StackItemType Type => StackItemType.Buffer;

        /// <summary>
        /// Create a buffer of the specified size.
        /// </summary>
        /// <param name="size">The size of this buffer.</param>
        /// <param name="zeroInitialize">Indicates whether the created buffer is zero-initialized.</param>
        public Buffer(int size, bool zeroInitialize = true)
        {
            InnerBuffer = zeroInitialize
                ? new byte[size]
                : GC.AllocateUninitializedArray<byte>(size);
        }

        /// <summary>
        /// Create a buffer with the specified data.
        /// </summary>
        /// <param name="data">The data to be contained in this buffer.</param>
        public Buffer(ReadOnlySpan<byte> data)
        {
            InnerBuffer = GC.AllocateUninitializedArray<byte>(data.Length);
            if (!data.IsEmpty) data.CopyTo(InnerBuffer);
        }

        public override StackItem ConvertTo(StackItemType type)
        {
            switch (type)
            {
                case StackItemType.Integer:
                    if (InnerBuffer.Length > Integer.MaxSize)
                        throw new InvalidCastException();
                    return new BigInteger(InnerBuffer);
                case StackItemType.ByteString:
                    byte[] clone = GC.AllocateUninitializedArray<byte>(InnerBuffer.Length);
                    InnerBuffer.CopyTo(clone.AsSpan());
                    return clone;
                default:
                    return base.ConvertTo(type);
            }
        }

        internal override StackItem DeepCopy(Dictionary<StackItem, StackItem> refMap)
        {
            if (refMap.TryGetValue(this, out StackItem? mappedItem)) return mappedItem;
            Buffer result = new(InnerBuffer);
            refMap.Add(this, result);
            return result;
        }

        public override bool GetBoolean()
        {
            return true;
        }

        public override ReadOnlySpan<byte> GetSpan()
        {
            return InnerBuffer;
        }
    }
}
