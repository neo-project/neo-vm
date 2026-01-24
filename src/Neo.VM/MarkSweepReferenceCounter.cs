// Copyright (C) 2015-2026 The Neo Project.
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
using System.Runtime.CompilerServices;
#if NET10_0_OR_GREATER
using System.Runtime.InteropServices;
using RuntimeUnsafe = System.Runtime.CompilerServices.Unsafe;
#endif

namespace Neo.VM;

/// <summary>
/// Reference counter that performs a deterministic mark-sweep collection.
/// </summary>
public sealed class MarkSweepReferenceCounter : IReferenceCounter
{
    private const int MaxPooledChildrenLists = 1024;
    private const int MaxPooledChildrenCapacity = 256;
    private const int MinChildrenByParentMutationsLimit = 256;
    private const int ChildrenByParentMutationLimitDivisor = 4;
    // Enable incremental root tracking only when the tracked graph is large enough to amortize updates.
    private const int StackRootsTrackingThreshold = 2048;
    private const int StackRootsTrackingRatioDivisor = 2;

    private readonly List<StackItem> trackedItems = new();
    private readonly List<StackItem> stackRoots = new();
    private readonly Stack<CompoundType> pending = new();
    private readonly Dictionary<CompoundType, List<StackItem>> childrenByParent = new(ReferenceEqualityComparer.Instance);
    private readonly Stack<List<StackItem>> childrenByParentPool = new();

    private int referencesCount;
    private int trackedStackReferences;
    private int zeroReferredCount;
    private int zeroReferredGeneration = 1;
    private int activeChildrenByParentEdges;
    private int childrenByParentMutations;
    private int childrenByParentMutationsLimit;
    private bool childrenByParentDirty = true;
    private int markGeneration;
    private bool useStackRoots;

    /// <inheritdoc/>
    public int Count => referencesCount;

    /// <inheritdoc/>
    public void AddZeroReferred(StackItem item)
    {
        if (item.ZeroReferredGeneration == zeroReferredGeneration) return;
        item.ZeroReferredGeneration = zeroReferredGeneration;
        zeroReferredCount++;
        if (NeedTrack(item))
            Track(item);
    }

    /// <inheritdoc/>
    public void AddReference(StackItem item, CompoundType parent)
    {
        referencesCount++;
        if (!NeedTrack(item)) return;

        Track(item);

        if (item.ObjectReferences is null)
        {
            var single = item.SingleObjectReference;
            if (single is null)
            {
                single = new StackItem.ObjectReferenceEntry(parent);
                item.SingleObjectReference = single;
                if (single.References++ == 0)
                {
                    if (ShouldDeferChildrenByParentUpdate())
                        single.ParentIndex = -1;
                    else
                        AddChildToParentList(parent, item, single);
                }
                return;
            }

            if (ReferenceEquals(single.Item, parent))
            {
                single.References++;
                return;
            }

            item.ObjectReferences = new Dictionary<CompoundType, StackItem.ObjectReferenceEntry>(2, ReferenceEqualityComparer.Instance)
            {
                { (CompoundType)single.Item, single }
            };
            item.SingleObjectReference = null;
        }

#if NET10_0_OR_GREATER
        ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(item.ObjectReferences, parent, out bool exists);
        if (!exists || entry is null)
            entry = new StackItem.ObjectReferenceEntry(parent);
        if (entry.References++ == 0)
        {
            if (ShouldDeferChildrenByParentUpdate())
                entry.ParentIndex = -1;
            else
                AddChildToParentList(parent, item, entry);
        }
#else
        if (!item.ObjectReferences.TryGetValue(parent, out var entry))
        {
            entry = new StackItem.ObjectReferenceEntry(parent);
            item.ObjectReferences.Add(parent, entry);
        }
        if (entry.References++ == 0)
        {
            if (ShouldDeferChildrenByParentUpdate())
                entry.ParentIndex = -1;
            else
                AddChildToParentList(parent, item, entry);
        }
#endif
    }

    /// <inheritdoc/>
    public void AddStackReference(StackItem item, int count = 1)
    {
        referencesCount += count;
        if (!NeedTrack(item)) return;

        Track(item);
        if (item.StackReferences == 0 && useStackRoots)
            AddStackRoot(item);
        item.StackReferences += count;
        trackedStackReferences += count;
        UnmarkZeroReferred(item);
    }

    /// <inheritdoc/>
    public int CheckZeroReferred()
    {
        if (zeroReferredCount == 0 || trackedItems.Count == 0)
            return referencesCount;

        Collect();
        return referencesCount;
    }

    /// <inheritdoc/>
    public void RemoveReference(StackItem item, CompoundType parent)
    {
        referencesCount--;
        if (!NeedTrack(item)) return;

        if (item.ObjectReferences is null)
        {
            var single = item.SingleObjectReference ?? throw new System.NullReferenceException();
            if (!ReferenceEquals(single.Item, parent))
                throw new KeyNotFoundException();
            single.References--;
            if (single.References == 0)
            {
                if (ShouldDeferChildrenByParentUpdate())
                    single.ParentIndex = -1;
                else
                    RemoveChildFromParentList(parent, item, single);
                item.SingleObjectReference = null;
            }
        }
        else
        {
#if NET10_0_OR_GREATER
            var references = item.ObjectReferences;
            ref var entry = ref CollectionsMarshal.GetValueRefOrNullRef(references, parent);
            if (RuntimeUnsafe.IsNullRef(ref entry))
                throw new KeyNotFoundException();
#else
            var references = item.ObjectReferences;
            var entry = references[parent];
#endif
            entry.References--;
            if (entry.References == 0)
            {
                if (ShouldDeferChildrenByParentUpdate())
                    entry.ParentIndex = -1;
                else
                    RemoveChildFromParentList(parent, item, entry);
                references.Remove(parent);
                if (references.Count == 1)
                {
                    foreach (var remaining in references.Values)
                    {
                        item.SingleObjectReference = remaining;
                        break;
                    }
                    item.ObjectReferences = null;
                }
                else if (references.Count == 0)
                {
                    item.ObjectReferences = null;
                }
            }
        }
        if (item.StackReferences == 0)
            MarkZeroReferred(item);
    }

    /// <inheritdoc/>
    public void RemoveStackReference(StackItem item)
    {
        referencesCount--;
        if (!NeedTrack(item)) return;

        trackedStackReferences--;
        if (--item.StackReferences == 0)
        {
            if (useStackRoots)
                RemoveStackRoot(item);
            MarkZeroReferred(item);
        }
    }

    /// <summary>
    /// Only compound types and buffers require tracking because they own other items or pooled memory.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool NeedTrack(StackItem item) => item is CompoundType or Buffer;

    private void Track(StackItem item)
    {
        if (item.MarkSweepIndex >= 0) return;
        item.MarkSweepIndex = trackedItems.Count;
        trackedItems.Add(item);
        item.MarkGeneration = 0;
        if (useStackRoots && item.StackReferences > 0)
            AddStackRoot(item);
    }

    private void AddStackRoot(StackItem item)
    {
        if (item.MarkSweepRootIndex >= 0) return;
        item.MarkSweepRootIndex = stackRoots.Count;
        stackRoots.Add(item);
    }

    private void RemoveStackRoot(StackItem item)
    {
        int index = item.MarkSweepRootIndex;
        if (index < 0) return;
        int lastIndex = stackRoots.Count - 1;
        if (index != lastIndex)
        {
            var lastItem = stackRoots[lastIndex];
            stackRoots[index] = lastItem;
            lastItem.MarkSweepRootIndex = index;
        }
        stackRoots.RemoveAt(lastIndex);
        item.MarkSweepRootIndex = -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MarkZeroReferred(StackItem item)
    {
        if (item.ZeroReferredGeneration == zeroReferredGeneration) return;
        item.ZeroReferredGeneration = zeroReferredGeneration;
        zeroReferredCount++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UnmarkZeroReferred(StackItem item)
    {
        if (item.ZeroReferredGeneration != zeroReferredGeneration) return;
        item.ZeroReferredGeneration = 0;
        zeroReferredCount--;
    }

    private void ClearZeroReferred()
    {
        if (zeroReferredCount == 0) return;
        zeroReferredCount = 0;
        if (zeroReferredGeneration == int.MaxValue)
        {
            foreach (var item in trackedItems)
                item.ZeroReferredGeneration = 0;
            zeroReferredGeneration = 1;
        }
        else
        {
            zeroReferredGeneration++;
        }
    }

    private void UpdateStackRootsTracking()
    {
        if (useStackRoots)
        {
            if (trackedItems.Count < StackRootsTrackingThreshold ||
                stackRoots.Count * StackRootsTrackingRatioDivisor > trackedItems.Count)
            {
                ClearStackRoots();
                useStackRoots = false;
            }
        }
        else if (trackedItems.Count >= StackRootsTrackingThreshold)
        {
            int rootCount = BuildStackRoots();
            if (rootCount * StackRootsTrackingRatioDivisor <= trackedItems.Count)
            {
                useStackRoots = true;
            }
            else
            {
                ClearStackRoots();
            }
        }
    }

    private int BuildStackRoots()
    {
        ClearStackRoots();
        foreach (var item in trackedItems)
        {
            if (item.StackReferences == 0) continue;
            item.MarkSweepRootIndex = stackRoots.Count;
            stackRoots.Add(item);
        }
        return stackRoots.Count;
    }

    private void ClearStackRoots()
    {
        foreach (var item in stackRoots)
            item.MarkSweepRootIndex = -1;
        stackRoots.Clear();
    }

    private void Collect()
    {
        AdvanceMarkGeneration();

        if (trackedStackReferences == 0)
        {
            // No stack roots means everything is unreachable.
            ClearZeroReferred();
            ClearChildrenByParent();
            ClearStackRoots();
            useStackRoots = false;
            while (trackedItems.Count > 0)
                RemoveTracked(trackedItems[^1], skipChildrenCleanup: true);
            return;
        }

        if (childrenByParentDirty)
            // Rebuild from ObjectReferences to include explicit AddReference edges.
            BuildChildrenByParent();

        UpdateStackRootsTracking();

        if (useStackRoots)
        {
            foreach (var item in stackRoots)
            {
                MarkUsingChildrenByParent(item);
            }
        }
        else
        {
            foreach (var item in trackedItems)
            {
                if (item.StackReferences == 0) continue;
                MarkUsingChildrenByParent(item);
            }
        }

        // Legacy Tarjan RC clears zero-referred up-front and then removes every
        // unmarked tracked item. To preserve identical semantics (and avoid
        // keeping unreachable descendants that were never re-added to zeroReferred),
        // we do the same here.
        ClearZeroReferred();

        for (int i = trackedItems.Count - 1; i >= 0; i--)
        {
            var item = trackedItems[i];
            if (item.MarkGeneration != markGeneration)
                RemoveTracked(item, skipChildrenCleanup: false);
        }
    }

    private void AdvanceMarkGeneration()
    {
        if (markGeneration == int.MaxValue)
        {
            foreach (var item in trackedItems)
                item.MarkGeneration = 0;
            markGeneration = 1;
            return;
        }

        markGeneration++;
    }

    private void AddChildToParentList(CompoundType parent, StackItem child, StackItem.ObjectReferenceEntry entry)
    {
#if NET10_0_OR_GREATER
        ref var children = ref CollectionsMarshal.GetValueRefOrAddDefault(childrenByParent, parent, out bool exists);
        if (!exists || children is null)
            children = GetChildrenList();
#else
        if (!childrenByParent.TryGetValue(parent, out var children))
        {
            children = GetChildrenList();
            childrenByParent.Add(parent, children);
        }
#endif

        entry.ParentIndex = children.Count;
        children.Add(child);
        activeChildrenByParentEdges++;
    }

    private void BuildChildrenByParent()
    {
        int edgeCount = 0;

        foreach (var children in childrenByParent.Values)
            children.Clear();

        foreach (var child in trackedItems)
        {
            var single = child.SingleObjectReference;
            if (single != null)
            {
                var parent = (CompoundType)single.Item;
#if NET10_0_OR_GREATER
                ref var children = ref CollectionsMarshal.GetValueRefOrAddDefault(childrenByParent, parent, out bool exists);
                if (!exists || children is null)
                    children = GetChildrenList();
#else
                if (!childrenByParent.TryGetValue(parent, out var children))
                {
                    children = GetChildrenList();
                    childrenByParent.Add(parent, children);
                }
#endif
                single.ParentIndex = children.Count;
                children.Add(child);
                edgeCount++;
            }

            if (child.ObjectReferences is null) continue;

            foreach (var pair in child.ObjectReferences)
            {
                var entry = pair.Value;
                var parent = pair.Key;

#if NET10_0_OR_GREATER
                ref var children = ref CollectionsMarshal.GetValueRefOrAddDefault(childrenByParent, parent, out bool exists);
                if (!exists || children is null)
                    children = GetChildrenList();
#else
                if (!childrenByParent.TryGetValue(parent, out var children))
                {
                    children = GetChildrenList();
                    childrenByParent.Add(parent, children);
                }
#endif

                entry.ParentIndex = children.Count;
                children.Add(child);
                edgeCount++;
            }
        }

        activeChildrenByParentEdges = edgeCount;
        childrenByParentDirty = false;
        ResetChildrenByParentMutations();
    }

    private void ClearChildrenByParent()
    {
        foreach (var children in childrenByParent.Values)
            ReturnChildrenList(children);
        childrenByParent.Clear();
        childrenByParentDirty = false;
        activeChildrenByParentEdges = 0;
        ResetChildrenByParentMutations();
    }

    // Switch to lazy rebuild after enough edge churn to amortize per-update cost.
    private bool ShouldDeferChildrenByParentUpdate()
    {
        if (childrenByParentDirty)
            return true;

        if (++childrenByParentMutations < childrenByParentMutationsLimit)
            return false;

        childrenByParentDirty = true;
        return true;
    }

    private void ResetChildrenByParentMutations()
    {
        childrenByParentMutations = 0;
        int scaledLimit = activeChildrenByParentEdges / ChildrenByParentMutationLimitDivisor;
        childrenByParentMutationsLimit = System.Math.Max(MinChildrenByParentMutationsLimit, scaledLimit);
    }

    private void RemoveChildFromParentList(CompoundType parent, StackItem child, StackItem.ObjectReferenceEntry entry)
    {
        if (!childrenByParent.TryGetValue(parent, out var children))
        {
            entry.ParentIndex = -1;
            return;
        }

        int index = entry.ParentIndex;
        if ((uint)index < (uint)children.Count && ReferenceEquals(children[index], child))
        {
            RemoveChildAt(children, index, parent);
        }
        else
        {
            int fallbackIndex = children.IndexOf(child);
            if (fallbackIndex >= 0)
                RemoveChildAt(children, fallbackIndex, parent);
        }

        entry.ParentIndex = -1;

        if (children.Count == 0)
        {
            childrenByParent.Remove(parent);
            ReturnChildrenList(children);
        }
    }

    private void RemoveChildAt(List<StackItem> children, int index, CompoundType parent)
    {
        int lastIndex = children.Count - 1;
        if (index != lastIndex)
        {
            var moved = children[lastIndex];
            children[index] = moved;
            var movedReferences = moved.ObjectReferences;
            if (movedReferences != null)
            {
                if (movedReferences.TryGetValue(parent, out var movedEntry))
                    movedEntry.ParentIndex = index;
            }
            else
            {
                var single = moved.SingleObjectReference;
                if (single != null && ReferenceEquals(single.Item, parent))
                    single.ParentIndex = index;
            }
        }
        children.RemoveAt(lastIndex);
        activeChildrenByParentEdges--;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RemoveParentReference(StackItem item, CompoundType parent)
    {
        var references = item.ObjectReferences;
        if (references != null)
        {
            if (!references.Remove(parent))
                return;
            if (references.Count == 1)
            {
                foreach (var remaining in references.Values)
                {
                    item.SingleObjectReference = remaining;
                    break;
                }
                item.ObjectReferences = null;
            }
            else if (references.Count == 0)
            {
                item.ObjectReferences = null;
            }
            return;
        }

        var single = item.SingleObjectReference;
        if (single != null && ReferenceEquals(single.Item, parent))
            item.SingleObjectReference = null;
    }


    private void MarkUsingChildrenByParent(StackItem root)
    {
        int currentGeneration = markGeneration;
        if (root.MarkGeneration == currentGeneration) return;
        root.MarkGeneration = currentGeneration;
        if (root is not CompoundType compoundRoot) return;
        pending.Push(compoundRoot);

        while (pending.Count > 0)
        {
            var parent = pending.Pop();
#if NET10_0_OR_GREATER
            ref var childrenRef = ref CollectionsMarshal.GetValueRefOrNullRef(childrenByParent, parent);
            if (RuntimeUnsafe.IsNullRef(ref childrenRef)) continue;
            var children = childrenRef;
#else
            if (!childrenByParent.TryGetValue(parent, out var children)) continue;
#endif

            int count = children.Count;
            for (int i = 0; i < count; i++)
            {
                var child = children[i];
                if (child.MarkGeneration == currentGeneration) continue;
                child.MarkGeneration = currentGeneration;
                if (child is CompoundType compoundChild)
                    pending.Push(compoundChild);
            }
        }
    }

    private void MarkUsingSubItems(StackItem root)
    {
        if (root.MarkGeneration == markGeneration) return;
        int currentGeneration = markGeneration;
        root.MarkGeneration = currentGeneration;
        if (root is not CompoundType compoundRoot) return;
        pending.Push(compoundRoot);

        while (pending.Count > 0)
        {
            var parent = pending.Pop();
            if (parent is Array array)
            {
                int count = array.Count;
                for (int i = 0; i < count; i++)
                {
                    var child = array[i];
                    if (child is CompoundType compoundChild)
                    {
                        if (compoundChild.MarkGeneration == currentGeneration) continue;
                        compoundChild.MarkGeneration = currentGeneration;
                        pending.Push(compoundChild);
                        continue;
                    }
                    if (child is Buffer buffer)
                    {
                        if (buffer.MarkGeneration == currentGeneration) continue;
                        buffer.MarkGeneration = currentGeneration;
                    }
                }
                continue;
            }
            if (parent is Map map)
            {
#if NET5_0_OR_GREATER
                foreach (var child in map.InternalDictionary.Values)
#else
                foreach (var pair in map.InternalDictionary)
#endif
                {
#if !NET5_0_OR_GREATER
                    var child = pair.Value;
#endif
                    if (child is CompoundType compoundChild)
                    {
                        if (compoundChild.MarkGeneration == currentGeneration) continue;
                        compoundChild.MarkGeneration = currentGeneration;
                        pending.Push(compoundChild);
                        continue;
                    }
                    if (child is Buffer buffer)
                    {
                        if (buffer.MarkGeneration == currentGeneration) continue;
                        buffer.MarkGeneration = currentGeneration;
                    }
                }
                continue;
            }

            foreach (var child in parent.SubItems)
            {
                if (child is CompoundType compoundChild)
                {
                    if (compoundChild.MarkGeneration == currentGeneration) continue;
                    compoundChild.MarkGeneration = currentGeneration;
                    pending.Push(compoundChild);
                    continue;
                }
                if (child is Buffer buffer)
                {
                    if (buffer.MarkGeneration == currentGeneration) continue;
                    buffer.MarkGeneration = currentGeneration;
                }
            }
        }
    }

    private void RemoveTracked(StackItem item, bool skipChildrenCleanup)
    {
        int index = item.MarkSweepIndex;
        if (index >= 0)
        {
            int lastIndex = trackedItems.Count - 1;
            if (index != lastIndex)
            {
                var lastItem = trackedItems[lastIndex];
                trackedItems[index] = lastItem;
                lastItem.MarkSweepIndex = index;
            }
            trackedItems.RemoveAt(lastIndex);
            item.MarkSweepIndex = -1;
        }
        UnmarkZeroReferred(item);
        if (useStackRoots && item.MarkSweepRootIndex >= 0)
            RemoveStackRoot(item);
        if (item.StackReferences > 0)
            trackedStackReferences -= item.StackReferences;
        item.StackReferences = 0;

        if (item is CompoundType compound)
        {
            List<StackItem>? children = null;
            bool useChildrenByParent = false;
            if (!skipChildrenCleanup && childrenByParent.Remove(compound, out var removedChildren))
            {
                if (!childrenByParentDirty)
                {
                    activeChildrenByParentEdges -= removedChildren.Count;
                    children = removedChildren;
                    useChildrenByParent = true;
                }
                else
                {
                    ReturnChildrenList(removedChildren);
                }
            }
            referencesCount -= compound.SubItemsCount;
            if (!skipChildrenCleanup)
            {
                if (useChildrenByParent && children != null)
                {
                    foreach (var child in children)
                        RemoveParentReference(child, compound);
                    ReturnChildrenList(children);
                }
                else
                {
                    int currentGeneration = markGeneration;
                    if (compound is Map map)
                    {
#if NET5_0_OR_GREATER
                        foreach (var child in map.InternalDictionary.Values)
#else
                        foreach (var pair in map.InternalDictionary)
#endif
                        {
#if !NET5_0_OR_GREATER
                            var child = pair.Value;
#endif
                            if (child.MarkGeneration != currentGeneration) continue;
                            RemoveParentReference(child, compound);
                        }
                    }
                    else if (compound is Array array)
                    {
                        int count = array.Count;
                        for (int i = 0; i < count; i++)
                        {
                            var child = array[i];
                            if (child.MarkGeneration != currentGeneration) continue;
                            RemoveParentReference(child, compound);
                        }
                    }
                    else
                    {
                        foreach (var child in compound.SubItems)
                        {
                            if (child.MarkGeneration != currentGeneration) continue;
                            RemoveParentReference(child, compound);
                        }
                    }
                }
            }
        }

        item.Cleanup();
    }

    private List<StackItem> GetChildrenList()
    {
        if (childrenByParentPool.Count > 0)
            return childrenByParentPool.Pop();
        return new List<StackItem>();
    }

    private void ReturnChildrenList(List<StackItem> children)
    {
        children.Clear();
        if (children.Capacity > MaxPooledChildrenCapacity) return;
        if (childrenByParentPool.Count < MaxPooledChildrenLists)
            childrenByParentPool.Push(children);
    }
}
