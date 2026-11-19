// Copyright (C) 2015-2026 The Neo Project.
//
// InteropInterface.cs file belongs to the neo project and is free
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
using System.Runtime.InteropServices;

namespace Neo.VM.Types;

/// <summary>
/// Represents an interface used to interoperate with the outside of the the VM.
/// </summary>
[DebuggerDisplay("Type={GetType().Name}, Value={_object}")]
public class InteropInterface : StackItem
{
    private readonly object _object;

    public override StackItemType Type => StackItemType.InteropInterface;

    /// <summary>
    /// Create an interoperability interface that wraps the specified <see cref="object"/>.
    /// </summary>
    /// <param name="value">The wrapped <see cref="object"/>.</param>
    public InteropInterface(object value)
    {
        _object = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override bool Equals(StackItem? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is InteropInterface i) return _object.Equals(i._object);
        return false;
    }

    public override bool GetBoolean()
    {
        return true;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_object);
    }

    public override T GetInterface<T>()
    {
        if (_object is T t) return t;
        throw new InvalidCastException($"This {nameof(InteropInterface)} can't be casted to type {typeof(T)}.");
    }

    public override string ToString()
    {
        return _object.ToString() ?? "NULL";
    }

    /// <summary>
    /// Blittable layout of the wrapped object, or UTF-8 type name when that is not possible.
    /// </summary>
    protected override ReadOnlySpan<byte> ComputeSpan(HashSet<StackItem> visited)
    {
        var ptr = nint.Zero;
        try
        {
            var size = Marshal.SizeOf(_object);
            if (size <= 0)
                return Utility.StrictUTF8.GetBytes(_object.GetType().Name);

            var bytes = GC.AllocateUninitializedArray<byte>(size);
            ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(_object, ptr, false);
            Marshal.Copy(ptr, bytes, 0, size);
            return bytes;
        }
        catch
        {
            return Utility.StrictUTF8.GetBytes(_object.GetType().Name);
        }
        finally
        {
            if (ptr != nint.Zero)
                Marshal.FreeHGlobal(ptr);
        }
    }
}
