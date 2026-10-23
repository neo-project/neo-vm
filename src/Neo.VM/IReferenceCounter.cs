// Copyright (C) 2015-2026 The Neo Project.
//
// IReferenceCounter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;

namespace Neo.VM;

/// <summary>
/// Used for reference counting of objects in the VM.
/// </summary>
public interface IReferenceCounter
{
    /// <summary>
    /// Gets the count of references.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Increments the number of references by the specified value. Use it carefully, this method does not
    /// perform any additional checks and changes RC value directly.
    /// </summary>
    void Inc(int count = 1);

    /// <summary>
    /// Decrements the number of references by the specified value. Use it carefully, this method does not
    /// perform any additional checks and changes RC value directly.
    /// </summary>
    void Dec(int count = 1);

    /// <summary>
    /// Adds a stack reference to a specified item with a count.
    ///
    /// Always increases <see cref="Count"/>. For compound types, the item's own
    /// reference count is increased, and children are counted only on the
    /// transition from unreferenced to referenced (0 → 1).
    /// </summary>
    /// <param name="item">The item to add a stack reference to.</param>
    /// <param name="count">The number of references to add.</param>
    void AddStackReference(StackItem item, int count = 1);

    /// <summary>
    /// Removes a stack reference from a specified item.
    ///
    /// Scalar items always decrease <see cref="Count"/>. Compound types are
    /// skipped when they are not referenced, which prevents a negative count
    /// on cyclic structures. Children are uncounted only when the item's last
    /// reference is dropped (1 → 0).
    /// </summary>
    /// <param name="item">The item to remove a stack reference from.</param>
    void RemoveStackReference(StackItem item);

    /// <summary>
    /// Validate reference counters after execution and throw if limits are violated.
    /// </summary>
    void PostExecuteInstruction();
}
