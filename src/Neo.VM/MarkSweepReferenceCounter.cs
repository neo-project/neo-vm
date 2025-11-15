// Copyright (C) 2015-2025 The Neo Project.
//
// MarkSweepReferenceCounter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System.Collections.Generic;

namespace Neo.VM;

/// <summary>
/// Reference counter that performs a deterministic mark-sweep collection.
/// </summary>
public sealed class MarkSweepReferenceCounter : IReferenceCounter
{
    private readonly HashSet<StackItem> trackedItems = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<StackItem> zeroReferred = new(ReferenceEqualityComparer.Instance);
    private readonly Stack<StackItem> pending = new();
    private readonly HashSet<StackItem> marked = new(ReferenceEqualityComparer.Instance);
    private readonly List<StackItem> unreachable = new();

    private int referencesCount;

    /// <inheritdoc/>
    public int Count => referencesCount;

    /// <inheritdoc/>
    public void AddZeroReferred(StackItem item)
    {
        zeroReferred.Add(item);
        if (NeedTrack(item))
            trackedItems.Add(item);
    }

    /// <inheritdoc/>
    public void AddReference(StackItem item, CompoundType parent)
    {
        referencesCount++;
        if (!NeedTrack(item)) return;

        Track(item);

        item.ObjectReferences ??= new Dictionary<CompoundType, StackItem.ObjectReferenceEntry>(ReferenceEqualityComparer.Instance);
        if (!item.ObjectReferences.TryGetValue(parent, out var entry))
        {
            entry = new StackItem.ObjectReferenceEntry(parent);
            item.ObjectReferences.Add(parent, entry);
        }
        entry.References++;
    }

    /// <inheritdoc/>
    public void AddStackReference(StackItem item, int count = 1)
    {
        referencesCount += count;
        if (!NeedTrack(item)) return;

        Track(item);
        item.StackReferences += count;
        zeroReferred.Remove(item);
    }

    /// <inheritdoc/>
    public int CheckZeroReferred()
    {
        if (zeroReferred.Count == 0 || trackedItems.Count == 0)
            return referencesCount;

        Collect();
        return referencesCount;
    }

    /// <inheritdoc/>
    public void RemoveReference(StackItem item, CompoundType parent)
    {
        referencesCount--;
        if (!NeedTrack(item)) return;

        item.ObjectReferences![parent].References--;
        if (item.StackReferences == 0)
            zeroReferred.Add(item);
    }

    /// <inheritdoc/>
    public void RemoveStackReference(StackItem item)
    {
        referencesCount--;
        if (!NeedTrack(item)) return;

        if (--item.StackReferences == 0)
            zeroReferred.Add(item);
    }

    private static bool NeedTrack(StackItem item) => item is CompoundType or Buffer;

    private void Track(StackItem item)
    {
        if (trackedItems.Add(item))
            zeroReferred.Add(item);
    }

    private void Collect()
    {
        marked.Clear();
        unreachable.Clear();

        foreach (var item in trackedItems)
        {
            if (item.StackReferences > 0)
                Mark(item);
        }

        foreach (var item in trackedItems)
        {
            if (!marked.Contains(item) && zeroReferred.Contains(item))
                unreachable.Add(item);
        }

        foreach (var item in unreachable)
            RemoveTracked(item);

        zeroReferred.RemoveWhere(marked.Contains);
    }

    private void Mark(StackItem root)
    {
        if (!marked.Add(root)) return;
        pending.Push(root);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (current.ObjectReferences is null) continue;
            foreach (var entry in current.ObjectReferences.Values)
            {
                if (entry.References <= 0) continue;
                if (marked.Add(entry.Item))
                    pending.Push(entry.Item);
            }
        }
    }

    private void RemoveTracked(StackItem item)
    {
        trackedItems.Remove(item);
        zeroReferred.Remove(item);
        item.StackReferences = 0;

        if (item is CompoundType compound)
        {
            referencesCount -= compound.SubItemsCount;
            foreach (var child in compound.SubItems)
            {
                if (!NeedTrack(child)) continue;
                child.ObjectReferences?.Remove(compound);
            }
        }

        item.ObjectReferences?.Clear();
        item.Cleanup();
    }
}
