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
    private readonly List<int> trackedMarkGenerations = new();
    private readonly List<int> trackedZeroReferredGenerations = new();
    private readonly Dictionary<StackItem, int> trackedIndices = new(ReferenceEqualityComparer.Instance);
    private readonly List<StackItem> stackRoots = new();
    private readonly Dictionary<StackItem, int> stackRootIndices = new(ReferenceEqualityComparer.Instance);
    private readonly Stack<CompoundType> pending = new();
    private readonly Dictionary<CompoundType, List<StackItem>> childrenByParent = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<EdgeKey, int> childrenByParentIndex = new(EdgeKeyComparer.Instance);
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
        if (!NeedTrack(item)) return;
        int index = Track(item);
        if (trackedZeroReferredGenerations[index] == zeroReferredGeneration) return;
        trackedZeroReferredGenerations[index] = zeroReferredGeneration;
        zeroReferredCount++;
    }

    /// <inheritdoc/>
    public void AddReference(StackItem item, CompoundType parent)
    {
        referencesCount++;
        if (!NeedTrack(item)) return;

        Track(item);

        item.ObjectReferences ??= new Dictionary<CompoundType, StackItem.ObjectReferenceEntry>(ReferenceEqualityComparer.Instance);
#if NET10_0_OR_GREATER
        ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(item.ObjectReferences, parent, out bool exists);
        if (!exists || entry is null)
            entry = new StackItem.ObjectReferenceEntry(parent);
        if (entry.References++ == 0)
        {
            if (ShouldDeferChildrenByParentUpdate())
                return;
            AddChildToParentList(parent, item);
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
                return;
            AddChildToParentList(parent, item);
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

        var references = item.ObjectReferences ?? throw new System.NullReferenceException();
#if NET10_0_OR_GREATER
        ref var entry = ref CollectionsMarshal.GetValueRefOrNullRef(references, parent);
        if (RuntimeUnsafe.IsNullRef(ref entry))
            throw new KeyNotFoundException();
#else
        var entry = references[parent];
#endif
        entry.References--;
        if (entry.References == 0)
        {
            if (!ShouldDeferChildrenByParentUpdate())
                RemoveChildFromParentList(parent, item);
            references.Remove(parent);
            if (references.Count == 0)
                item.ObjectReferences = null;
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

    private int Track(StackItem item)
    {
        if (trackedIndices.TryGetValue(item, out int index))
            return index;

        index = trackedItems.Count;
        trackedIndices[item] = index;
        trackedItems.Add(item);
        trackedMarkGenerations.Add(0);
        trackedZeroReferredGenerations.Add(0);
        if (useStackRoots && item.StackReferences > 0)
            AddStackRoot(item);
        return index;
    }

    private void AddStackRoot(StackItem item)
    {
        if (stackRootIndices.ContainsKey(item)) return;
        stackRootIndices[item] = stackRoots.Count;
        stackRoots.Add(item);
    }

    private void RemoveStackRoot(StackItem item)
    {
        if (!stackRootIndices.TryGetValue(item, out int index)) return;
        int lastIndex = stackRoots.Count - 1;
        if (index != lastIndex)
        {
            var lastItem = stackRoots[lastIndex];
            stackRoots[index] = lastItem;
            stackRootIndices[lastItem] = index;
        }
        stackRoots.RemoveAt(lastIndex);
        stackRootIndices.Remove(item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MarkZeroReferred(StackItem item)
    {
        if (!trackedIndices.TryGetValue(item, out int index)) return;
        if (trackedZeroReferredGenerations[index] == zeroReferredGeneration) return;
        trackedZeroReferredGenerations[index] = zeroReferredGeneration;
        zeroReferredCount++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UnmarkZeroReferred(StackItem item)
    {
        if (!trackedIndices.TryGetValue(item, out int index)) return;
        if (trackedZeroReferredGenerations[index] != zeroReferredGeneration) return;
        trackedZeroReferredGenerations[index] = 0;
        zeroReferredCount--;
    }

    private void ClearZeroReferred()
    {
        if (zeroReferredCount == 0) return;
        zeroReferredCount = 0;
        if (zeroReferredGeneration == int.MaxValue)
        {
            for (int i = 0; i < trackedZeroReferredGenerations.Count; i++)
                trackedZeroReferredGenerations[i] = 0;
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
            stackRootIndices[item] = stackRoots.Count;
            stackRoots.Add(item);
        }
        return stackRoots.Count;
    }

    private void ClearStackRoots()
    {
        stackRoots.Clear();
        stackRootIndices.Clear();
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
            if (trackedMarkGenerations[i] != markGeneration)
                RemoveTracked(trackedItems[i], skipChildrenCleanup: false);
        }
    }

    private void AdvanceMarkGeneration()
    {
        if (markGeneration == int.MaxValue)
        {
            for (int i = 0; i < trackedMarkGenerations.Count; i++)
                trackedMarkGenerations[i] = 0;
            markGeneration = 1;
            return;
        }

        markGeneration++;
    }

    private void AddChildToParentList(CompoundType parent, StackItem child)
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

        childrenByParentIndex[new EdgeKey(parent, child)] = children.Count;
        children.Add(child);
        activeChildrenByParentEdges++;
    }

    private void BuildChildrenByParent()
    {
        int edgeCount = 0;

        childrenByParentIndex.Clear();
        foreach (var children in childrenByParent.Values)
            children.Clear();

        foreach (var child in trackedItems)
        {
            var references = child.ObjectReferences;
            if (references is null) continue;

            foreach (var pair in references)
            {
                var entry = pair.Value;
                if (entry.References == 0) continue;
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

                childrenByParentIndex[new EdgeKey(parent, child)] = children.Count;
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
        childrenByParentIndex.Clear();
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

    private void RemoveChildFromParentList(CompoundType parent, StackItem child)
    {
        if (!childrenByParent.TryGetValue(parent, out var children))
            return;

        var key = new EdgeKey(parent, child);
        if (childrenByParentIndex.TryGetValue(key, out int index) &&
            (uint)index < (uint)children.Count &&
            ReferenceEquals(children[index], child))
        {
            RemoveChildAt(children, index, parent, child);
        }
        else
        {
            int fallbackIndex = children.IndexOf(child);
            if (fallbackIndex >= 0)
                RemoveChildAt(children, fallbackIndex, parent, child);
        }

        if (children.Count == 0)
        {
            childrenByParent.Remove(parent);
            ReturnChildrenList(children);
        }
    }

    private void RemoveChildAt(List<StackItem> children, int index, CompoundType parent, StackItem child)
    {
        int lastIndex = children.Count - 1;
        if (index != lastIndex)
        {
            var moved = children[lastIndex];
            children[index] = moved;
            childrenByParentIndex[new EdgeKey(parent, moved)] = index;
        }
        children.RemoveAt(lastIndex);
        childrenByParentIndex.Remove(new EdgeKey(parent, child));
        activeChildrenByParentEdges--;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RemoveParentReference(StackItem item, CompoundType parent)
    {
        var references = item.ObjectReferences;
        if (references == null) return;
        if (!references.Remove(parent))
            return;
        if (references.Count == 0)
            item.ObjectReferences = null;
    }


    private void MarkUsingChildrenByParent(StackItem root)
    {
        int currentGeneration = markGeneration;
        if (!trackedIndices.TryGetValue(root, out int rootIndex)) return;
        if (trackedMarkGenerations[rootIndex] == currentGeneration) return;
        trackedMarkGenerations[rootIndex] = currentGeneration;
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
                if (!trackedIndices.TryGetValue(child, out int childIndex)) continue;
                if (trackedMarkGenerations[childIndex] == currentGeneration) continue;
                trackedMarkGenerations[childIndex] = currentGeneration;
                if (child is CompoundType compoundChild)
                    pending.Push(compoundChild);
            }
        }
    }

    private void MarkUsingSubItems(StackItem root)
    {
        int currentGeneration = markGeneration;
        if (!trackedIndices.TryGetValue(root, out int rootIndex)) return;
        if (trackedMarkGenerations[rootIndex] == currentGeneration) return;
        trackedMarkGenerations[rootIndex] = currentGeneration;
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
                        if (!trackedIndices.TryGetValue(compoundChild, out int childIndex)) continue;
                        if (trackedMarkGenerations[childIndex] == currentGeneration) continue;
                        trackedMarkGenerations[childIndex] = currentGeneration;
                        pending.Push(compoundChild);
                        continue;
                    }
                    if (child is Buffer buffer)
                    {
                        if (!trackedIndices.TryGetValue(buffer, out int bufferIndex)) continue;
                        if (trackedMarkGenerations[bufferIndex] == currentGeneration) continue;
                        trackedMarkGenerations[bufferIndex] = currentGeneration;
                    }
                }
                continue;
            }
            if (parent is Map map)
            {
                foreach (var child in map.Values)
                {
                    if (child is CompoundType compoundChild)
                    {
                        if (!trackedIndices.TryGetValue(compoundChild, out int childIndex)) continue;
                        if (trackedMarkGenerations[childIndex] == currentGeneration) continue;
                        trackedMarkGenerations[childIndex] = currentGeneration;
                        pending.Push(compoundChild);
                        continue;
                    }
                    if (child is Buffer buffer)
                    {
                        if (!trackedIndices.TryGetValue(buffer, out int bufferIndex)) continue;
                        if (trackedMarkGenerations[bufferIndex] == currentGeneration) continue;
                        trackedMarkGenerations[bufferIndex] = currentGeneration;
                    }
                }
                continue;
            }

            foreach (var child in parent.SubItems)
            {
                if (child is CompoundType compoundChild)
                {
                    if (!trackedIndices.TryGetValue(compoundChild, out int childIndex)) continue;
                    if (trackedMarkGenerations[childIndex] == currentGeneration) continue;
                    trackedMarkGenerations[childIndex] = currentGeneration;
                    pending.Push(compoundChild);
                    continue;
                }
                if (child is Buffer buffer)
                {
                    if (!trackedIndices.TryGetValue(buffer, out int bufferIndex)) continue;
                    if (trackedMarkGenerations[bufferIndex] == currentGeneration) continue;
                    trackedMarkGenerations[bufferIndex] = currentGeneration;
                }
            }
        }
    }

    private void RemoveTracked(StackItem item, bool skipChildrenCleanup)
    {
        UnmarkZeroReferred(item);
        if (useStackRoots)
            RemoveStackRoot(item);
        if (item.StackReferences > 0)
            trackedStackReferences -= item.StackReferences;
        item.StackReferences = 0;
        if (trackedIndices.TryGetValue(item, out int index))
        {
            int lastIndex = trackedItems.Count - 1;
            if (index != lastIndex)
            {
                var lastItem = trackedItems[lastIndex];
                trackedItems[index] = lastItem;
                trackedIndices[lastItem] = index;
                trackedMarkGenerations[index] = trackedMarkGenerations[lastIndex];
                trackedZeroReferredGenerations[index] = trackedZeroReferredGenerations[lastIndex];
            }
            trackedItems.RemoveAt(lastIndex);
            trackedMarkGenerations.RemoveAt(lastIndex);
            trackedZeroReferredGenerations.RemoveAt(lastIndex);
            trackedIndices.Remove(item);
        }

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
                    int currentGeneration = markGeneration;
                    foreach (var child in children)
                    {
                        childrenByParentIndex.Remove(new EdgeKey(compound, child));
                        if (!trackedIndices.TryGetValue(child, out int childIndex)) continue;
                        if (trackedMarkGenerations[childIndex] != currentGeneration) continue;
                        RemoveParentReference(child, compound);
                    }
                    ReturnChildrenList(children);
                }
                else
                {
                    int currentGeneration = markGeneration;
                    if (compound is Map map)
                    {
                        foreach (var child in map.Values)
                        {
                            if (!trackedIndices.TryGetValue(child, out int childIndex)) continue;
                            if (trackedMarkGenerations[childIndex] != currentGeneration) continue;
                            RemoveParentReference(child, compound);
                        }
                    }
                    else if (compound is Array array)
                    {
                        int count = array.Count;
                        for (int i = 0; i < count; i++)
                        {
                            var child = array[i];
                            if (!trackedIndices.TryGetValue(child, out int childIndex)) continue;
                            if (trackedMarkGenerations[childIndex] != currentGeneration) continue;
                            RemoveParentReference(child, compound);
                        }
                    }
                    else
                    {
                        foreach (var child in compound.SubItems)
                        {
                            if (!trackedIndices.TryGetValue(child, out int childIndex)) continue;
                            if (trackedMarkGenerations[childIndex] != currentGeneration) continue;
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

    private readonly struct EdgeKey
    {
        public readonly CompoundType Parent;
        public readonly StackItem Child;

        public EdgeKey(CompoundType parent, StackItem child)
        {
            Parent = parent;
            Child = child;
        }
    }

    private sealed class EdgeKeyComparer : IEqualityComparer<EdgeKey>
    {
        public static readonly EdgeKeyComparer Instance = new();

        public bool Equals(EdgeKey x, EdgeKey y)
            => ReferenceEquals(x.Parent, y.Parent) && ReferenceEquals(x.Child, y.Child);

        public int GetHashCode(EdgeKey obj)
        {
            unchecked
            {
                int hash = RuntimeHelpers.GetHashCode(obj.Parent);
                return (hash * 397) ^ RuntimeHelpers.GetHashCode(obj.Child);
            }
        }
    }
}
