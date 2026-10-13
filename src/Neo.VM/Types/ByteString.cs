// Copyright (C) 2015-2026 The Neo Project.
//
// ByteString.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types;

/// <summary>
/// Represents an immutable memory block in the VM.
/// </summary>
[DebuggerDisplay("Type={GetType().Name}, Value={System.Convert.ToHexString(GetSpan())}")]
public class ByteString : PrimitiveType
{
    /// <summary>
    /// An empty <see cref="ByteString"/>.
    /// </summary>
    public static readonly ByteString Empty = ReadOnlyMemory<byte>.Empty;

    private IMemoryOwner<byte>? _memoryOwner;
    private bool _keepAlive;
    private bool _disposed;
    private int _stackRefs;

    public override ReadOnlyMemory<byte> Memory { get; }
    public override StackItemType Type => StackItemType.ByteString;

    internal bool HasOwner => _memoryOwner is not null;

    /// <summary>
    /// Create a new <see cref="ByteString"/> with the specified data.
    /// Copies into pooled memory so the caller may mutate <paramref name="data"/>.
    /// </summary>
    /// <param name="data">The data to be contained in this <see cref="ByteString"/>.</param>
    public ByteString(ReadOnlyMemory<byte> data)
    {
        if (data.Length == 0)
        {
            Memory = ReadOnlyMemory<byte>.Empty;
            return;
        }
        _memoryOwner = MemoryPool<byte>.Shared.Rent(data.Length);
        data.Span.CopyTo(_memoryOwner.Memory.Span);
        Memory = _memoryOwner.Memory[..data.Length];
    }

    // Distinct from the public copy ctor (same payload type).
    private ByteString(ReadOnlyMemory<byte> immutable, bool alias)
    {
        Memory = immutable.Length == 0 ? ReadOnlyMemory<byte>.Empty : immutable;
    }

    /// <summary>
    /// Wrap trusted immutable bytes (script <c>PUSHDATA</c> operands) without copying.
    /// </summary>
    internal static ByteString FromImmutable(ReadOnlyMemory<byte> data)
        => data.Length == 0 ? Empty : new ByteString(data, alias: true);

    /// <summary>
    /// Keep pooled memory after the last stack reference is dropped
    /// (result stack, notifications, host <c>Pop</c>).
    /// </summary>
    public void KeepAlive()
        => _keepAlive = true;

    internal void AddOwnerRefs(int count)
        => _stackRefs += count;

    internal void ReleaseOwnerRef()
    {
        if (_stackRefs > 0)
            _stackRefs--;
        if (_stackRefs == 0)
            Cleanup();
    }

    internal override void Cleanup()
    {
        if (_keepAlive || _disposed)
            return;
        _disposed = true;
        _memoryOwner?.Dispose();
        _memoryOwner = null;
    }

    private bool Equals(ByteString other)
    {
        return GetSpan().SequenceEqual(other.GetSpan());
    }

    public override bool Equals(StackItem? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is not ByteString b) return false;
        return Equals(b);
    }

    internal override bool Equals(StackItem? other, ExecutionEngineLimits limits)
    {
        uint maxComparableSize = limits.MaxComparableSize;
        return Equals(other, ref maxComparableSize);
    }

    internal bool Equals(StackItem? other, ref uint limits)
    {
        if (Size > limits || limits == 0)
            throw new InvalidOperationException($"The operand exceeds the maximum comparable size, {Size}/{limits}.");
        uint comparedSize = 1;
        try
        {
            if (other is not ByteString b) return false;
            comparedSize = Math.Max((uint)Math.Max(Size, b.Size), comparedSize);
            if (ReferenceEquals(this, b)) return true;
            if (b.Size > limits)
                throw new InvalidOperationException($"The operand exceeds the maximum comparable size, {b.Size}/{limits}.");
            return Equals(b);
        }
        finally
        {
            limits -= comparedSize;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool GetBoolean()
    {
        if (Size > Integer.MaxSize) throw new InvalidCastException();
        return GetSpan().NotZero();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override BigInteger GetInteger()
    {
        if (Size > Integer.MaxSize) throw new InvalidCastException($"Can not convert {nameof(ByteString)} to an integer, MaxSize of {nameof(Integer)} is exceeded: {Size}/{Integer.MaxSize}.");
        return new BigInteger(GetSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlyMemory<byte>(ByteString value)
    {
        return value.Memory;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<byte>(ByteString value)
    {
        return value.Memory.Span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ByteString(byte[] value)
    {
        return new ByteString(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ByteString(ReadOnlyMemory<byte> value)
    {
        return new ByteString(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ByteString(string value)
    {
        return new ByteString(value.ToStrictUtf8Bytes());
    }

    public override string ToString()
    {
        return GetSpan().TryToStrictUtf8String(out var str)
            ? $"\"{str}\""
            : $"\"Base64: {Convert.ToBase64String(GetSpan())}\"";
    }
}
