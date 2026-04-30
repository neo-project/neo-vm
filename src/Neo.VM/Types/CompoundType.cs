// Copyright (C) 2015-2026 The Neo Project.
//
// CompoundType.cs file belongs to the neo project and is free
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

namespace Neo.VM.Types;

/// <summary>
/// The base class for complex types in the VM.
/// </summary>
[DebuggerDisplay("Type={GetType().Name}, Count={Count}, Id={System.Collections.Generic.ReferenceEqualityComparer.Instance.GetHashCode(this)}")]
public abstract class CompoundType : StackItem
{
    /// <summary>
    /// Create a new <see cref="CompoundType"/>.
    /// </summary>
    protected CompoundType() { }

    /// <summary>
    /// The number of references to this StackItem from the evaluation stack.
    ///
    /// This field tracks how many times this item is referenced by the evaluation stack.
    /// It is incremented when the item is pushed onto the stack and decremented when it is popped off.
    ///
    /// Use this field to manage stack references and determine when an item is no longer needed.
    /// </summary>
    internal int StackReferences = 0;

    /// <summary>
    /// Indicates whether this <see cref="StackItem"/> is referenced from any VM stack roots.
    /// </summary>
    internal bool IsStackReferenced => StackReferences != 0;

    /// <summary>
    /// The number of items in this VM object.
    /// </summary>
    public abstract int Count { get; }

    public abstract IEnumerable<StackItem> SubItems { get; }

    public abstract int SubItemsCount { get; }

    public bool IsReadOnly { get; protected set; }

    /// <summary>
    /// Remove all items from the VM object.
    /// </summary>
    public abstract void Clear();

    internal abstract override StackItem DeepCopy(Dictionary<StackItem, StackItem> refMap, bool asImmutable);

    public sealed override bool GetBoolean()
    {
        return true;
    }

    public override int GetHashCode() => throw new NotSupportedException("Mutable compound type does not support GetHashCode.");

    public override string ToString()
    {
        return Count.ToString();
    }
}
