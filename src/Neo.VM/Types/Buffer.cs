// Copyright (C) 2015-2026 The Neo Project.
//
// Buffer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;

namespace Neo.VM.Types;

/// <summary>
/// Represents a memory block that can be used for reading and writing in the VM.
/// </summary>
[DebuggerDisplay("Type={GetType().Name}, Value={System.Convert.ToHexString(GetSpan())}")]
public class Buffer : StackItem
{
    /// <summary>
    /// The internal byte array used to store the actual data.
    /// </summary>
    public readonly Memory<byte> InnerBuffer;

    /// <summary>
    /// The size of the buffer.
    /// </summary>
    public int Size => InnerBuffer.Length;
    public override StackItemType Type => StackItemType.Buffer;

    /// <summary>
    /// Create a buffer of the specified size.
    /// </summary>
    /// <param name="size">The size of this buffer.</param>
    public Buffer(int size)
    {
        var stream = new MemoryStream(size);
        stream.SetLength(size);
        InnerBuffer = new Memory<byte>(stream.GetBuffer(), 0, size);
    }

    /// <summary>
    /// Create a buffer with the specified data.
    /// </summary>
    /// <param name="data">The data to be contained in this buffer.</param>
    public Buffer(ReadOnlySpan<byte> data) : this(data.Length)
    {
        data.CopyTo(InnerBuffer.Span);
    }

    public override StackItem ConvertTo(StackItemType type)
    {
        switch (type)
        {
            case StackItemType.Integer:
                if (InnerBuffer.Length > Integer.MaxSize)
                    throw new InvalidCastException();
                return new BigInteger(InnerBuffer.Span);
            case StackItemType.ByteString:
                byte[] clone = GC.AllocateUninitializedArray<byte>(InnerBuffer.Length);
                InnerBuffer.CopyTo(clone);
                return clone;
            default:
                return base.ConvertTo(type);
        }
    }

    internal override StackItem DeepCopy(Dictionary<StackItem, StackItem> refMap, bool asImmutable)
    {
        if (refMap.TryGetValue(this, out StackItem? mappedItem)) return mappedItem;
        StackItem result = asImmutable ? new ByteString(InnerBuffer.ToArray()) : new Buffer(InnerBuffer.Span);
        refMap.Add(this, result);
        return result;
    }

    public override bool GetBoolean()
    {
        return true;
    }

    public override ReadOnlySpan<byte> GetSpan()
    {
        return InnerBuffer.Span;
    }

    public override string ToString()
    {
        return GetSpan().TryToStrictUtf8String(out var str)
            ? $"(\"{str}\")"
            : $"(\"Base64: {Convert.ToBase64String(GetSpan())}\")";
    }

    public override int GetHashCode() => throw new NotSupportedException("Mutable buffer does not support GetHashCode.");
}
