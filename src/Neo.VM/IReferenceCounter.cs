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
    /// Reference Counter version
    /// </summary>
    RCVersion Version { get; }

    /// <summary>
    /// Gets the count of references.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Adds a stack reference to a specified item with a count.
    ///
    /// This method is used when an item gains a new stack reference, usually due to being pushed onto the evaluation stack.
    /// It increments the reference count and updates the tracking structures if necessary.
    ///
    /// Use this method when you need to add one or more stack references to a stack item.
    /// </summary>
    /// <param name="item">The item to add a stack reference to.</param>
    /// <param name="count">The number of references to add.</param>
    void AddStackReference(StackItem item, int count = 1);

    /// <summary>
    /// Removes a stack reference from a specified item.
    ///
    /// This method is used when an item loses a stack reference, usually due to being popped off the evaluation stack.
    /// It decrements the reference count and updates the tracking structures if necessary.
    ///
    /// Use this method when you need to remove one or more stack references from a stack item.
    /// </summary>
    /// <param name="item">The item to remove a stack reference from.</param>
    void RemoveStackReference(StackItem item);

    /// <summary>
    /// Validate reference counters after execution and throw if limits are violated.
    /// </summary>
    void PostExecuteInstruction();
}
