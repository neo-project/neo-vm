// Copyright (C) 2015-2026 The Neo Project.
//
// Boolean.cs file belongs to the neo project and is free
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
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types;

/// <summary>
/// Represents a boolean (<see langword="true" /> or <see langword="false" />) value in the VM.
/// </summary>
[DebuggerDisplay("Type={GetType().Name}, Value={value}")]
public class Boolean : PrimitiveType
{
    private static readonly ReadOnlyMemory<byte> TRUE = new byte[] { 1 };
    private static readonly ReadOnlyMemory<byte> FALSE = new byte[] { 0 };

    private readonly bool value;

    public override ReadOnlyMemory<byte> Memory => value ? TRUE : FALSE;
    public override int Size => sizeof(bool);
    public override StackItemType Type => StackItemType.Boolean;

    /// <summary>
    /// Create a new VM object representing the boolean type.
    /// </summary>
    /// <param name="value">The initial value of the object.</param>
    internal Boolean(bool value)
    {
        this.value = value;
    }

    public override bool Equals(StackItem? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is Boolean b) return value == b.value;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool GetBoolean()
    {
        return value;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override BigInteger GetInteger()
    {
        return value ? BigInteger.One : BigInteger.Zero;
    }

    /// <summary>
    /// One byte: <c>1</c> if true, <c>0</c> if false.
    /// </summary>
    protected override ReadOnlySpan<byte> ComputeSpan(HashSet<StackItem> visited)
        => value ? [1] : [0];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Boolean(bool value)
    {
        return value ? True : False;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator bool(Boolean value) => value.GetBoolean();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator BigInteger(Boolean value)
        => value.GetInteger();

    public override string ToString()
    {
        return value.ToString();
    }
}
