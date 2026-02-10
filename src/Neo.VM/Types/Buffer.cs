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
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
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
    private readonly Memory<byte> _innerBuffer;

    /// <summary>
    /// The size of the buffer.
    /// </summary>
    public int Size => _innerBuffer.Length;
    public override StackItemType Type => StackItemType.Buffer;

    private readonly byte[] _buffer;
    private bool _keep_alive = false;

    /// <summary>
    /// Create a buffer of the specified size.
    /// </summary>
    /// <param name="size">The size of this buffer.</param>
    /// <param name="zeroInitialize">Indicates whether the created buffer is zero-initialized.</param>
    public Buffer(int size, bool zeroInitialize = true)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(size);
        _innerBuffer = new Memory<byte>(_buffer, 0, size);
        if (zeroInitialize) _innerBuffer.Span.Clear();
    }

    /// <summary>
    /// Create a buffer with the specified data.
    /// </summary>
    /// <param name="data">The data to be contained in this buffer.</param>
    public Buffer(ReadOnlySpan<byte> data) : this(data.Length, false)
    {
        data.CopyTo(_innerBuffer.Span);
    }

    internal override void Cleanup()
    {
        if (!_keep_alive)
            ArrayPool<byte>.Shared.Return(_buffer, clearArray: false);
    }

    public void KeepAlive()
    {
        _keep_alive = true;
    }

    public override StackItem ConvertTo(StackItemType type)
    {
        switch (type)
        {
            case StackItemType.Integer:
                if (_innerBuffer.Length > Integer.MaxSize)
                    throw new InvalidCastException();
                return new BigInteger(_innerBuffer.Span);
            case StackItemType.ByteString:
#if NET5_0_OR_GREATER
                byte[] clone = GC.AllocateUninitializedArray<byte>(_innerBuffer.Length);
#else
                byte[] clone = new byte[_innerBuffer.Length];
#endif
                _innerBuffer.CopyTo(clone);
                return clone;
            default:
                return base.ConvertTo(type);
        }
    }

    internal override StackItem DeepCopy(Dictionary<StackItem, StackItem> refMap, bool asImmutable)
    {
        if (refMap.TryGetValue(this, out StackItem? mappedItem)) return mappedItem;
        StackItem result = asImmutable ? new ByteString(_innerBuffer.ToArray()) : new Buffer(_innerBuffer.Span);
        refMap.Add(this, result);
        return result;
    }

    public override bool GetBoolean()
    {
        return true;
    }

    public override ReadOnlySpan<byte> GetSpan()
    {
        return _innerBuffer.Span;
    }

    public override string ToString()
    {
        return GetSpan().TryToStrictUtf8String(out var str)
            ? $"(\"{str}\")"
            : $"(\"Base64: {Convert.ToBase64String(GetSpan())}\")";
    }

    #region Write operations

    public void Set(int index, byte value)
    {
        _innerBuffer.Span[index] = value;
        _hashCode = 0;
    }

    public void Reverse()
    {
        _innerBuffer.Span.Reverse();
        _hashCode = 0;
    }

    public void CopyInto(ReadOnlySpan<byte> span)
    {
        span.CopyTo(_innerBuffer.Span);
        _hashCode = 0;
    }

    public void CopyInto(ReadOnlySpan<byte> span, int index)
    {
        span.CopyTo(_innerBuffer.Span[index..]);
        _hashCode = 0;
    }

    #endregion
}
