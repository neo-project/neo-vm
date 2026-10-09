// Copyright (C) 2015-2026 The Neo Project.
//
// Integer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types;

/// <summary>
/// Represents an integer value in the VM.
/// </summary>
[DebuggerDisplay("Type={GetType().Name}, Value={value}")]
public class Integer : PrimitiveType
{
    /// <summary>
    /// The maximum size of an integer in bytes.
    /// </summary>
    public const int MaxSize = 32;

    /// <summary>
    /// Represents the number 0.
    /// </summary>
    public static readonly Integer Zero = 0;
    private readonly BigInteger value;

    public override ReadOnlyMemory<byte> Memory => value.IsZero ? ReadOnlyMemory<byte>.Empty : value.ToByteArray();
    public override int Size { get; }
    public override StackItemType Type => StackItemType.Integer;

    /// <summary>
    /// Create an integer with the specified value.
    /// </summary>
    /// <param name="value">The value of the integer.</param>
    public Integer(BigInteger value)
    {
        if (value.IsZero)
        {
            Size = 0;
        }
        else
        {
            Size = value.GetByteCount();
            if (Size > MaxSize) throw new ArgumentException($"Integer size {Size} bytes exceeds maximum allowed size of {MaxSize} bytes.", nameof(value));
        }
        this.value = value;
    }

    public override bool Equals(StackItem? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is Integer i) return value == i.value;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool GetBoolean()
    {
        return !value.IsZero;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override BigInteger GetInteger()
    {
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Integer(sbyte value)
    {
        return (BigInteger)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Integer(byte value)
    {
        return (BigInteger)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Integer(short value)
    {
        return (BigInteger)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Integer(ushort value)
    {
        return (BigInteger)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Integer(int value)
    {
        return (BigInteger)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Integer(uint value)
    {
        return (BigInteger)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Integer(long value)
    {
        return (BigInteger)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Integer(ulong value)
    {
        return (BigInteger)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Integer(BigInteger value)
    {
        return new Integer(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator BigInteger(Integer value)
        => value.GetInteger();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator byte(Integer value) => (byte)value.GetInteger();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator sbyte(Integer value) => (sbyte)value.GetInteger();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator short(Integer value) => (short)value.GetInteger();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator ushort(Integer value) => (ushort)value.GetInteger();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator int(Integer value) => (int)value.GetInteger();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator uint(Integer value) => (uint)value.GetInteger();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator long(Integer value) => (long)value.GetInteger();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator ulong(Integer value) => (ulong)value.GetInteger();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator bool(Integer value) => value.GetBoolean();

    public override string ToString()
    {
        return value.ToString();
    }
}
