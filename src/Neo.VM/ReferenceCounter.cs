// Copyright (C) 2016-2022 The Neo Project.
// 
// The neo-vm is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.StronglyConnectedComponents;
using Neo.VM.Types;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Neo.VM
{
    /// <summary>
    /// Used for reference counting of objects in the VM.
    /// </summary>
    public sealed class ReferenceCounter
    {
        private readonly HashSet<StackItem> tracked_items = new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<StackItem> zero_referred = new(ReferenceEqualityComparer.Instance);
        private LinkedList<HashSet<StackItem>>? cached_components;
        private int references_count = 0;

        /// <summary>
        /// Indicates the number of this counter.
        /// </summary>
        public int Count => references_count;

        internal void AddReference(StackItem item, CompoundType parent)
        {
            references_count++;
            cached_components = null;
            tracked_items.Add(item);
            if (item.ObjectReferences is null)
            {
                var dict = new ConcurrentDictionary<CompoundType, StackItem.ObjectReferenceEntry>(ReferenceEqualityComparer.Instance);
                Interlocked.CompareExchange(ref item.ObjectReferences, dict, null);
            }

            var pEntry = item.ObjectReferences.GetOrAdd(parent, static parent => new(parent));
            pEntry.References++;
        }

        internal void AddStackReference(StackItem item, int count = 1)
        {
            references_count += count;
            if (tracked_items.Add(item))
                cached_components?.AddLast(new HashSet<StackItem>(ReferenceEqualityComparer.Instance) { item });
            item.StackReferences += count;
            zero_referred.Remove(item);
        }

        internal void AddZeroReferred(StackItem item)
        {
            zero_referred.Add(item);
            cached_components?.AddLast(new HashSet<StackItem>(ReferenceEqualityComparer.Instance) { item });
            tracked_items.Add(item);
        }

        internal int CheckZeroReferred()
        {
            if (zero_referred.Count > 0)
            {
                zero_referred.Clear();
                if (cached_components is null)
                {
                    //Tarjan<StackItem> tarjan = new(tracked_items.Where(p => p.StackReferences == 0));
                    Tarjan tarjan = new(tracked_items);
                    cached_components = tarjan.Invoke();
                }
                foreach (StackItem item in tracked_items)
                    item.Reset();
                for (var node = cached_components.First; node != null;)
                {
                    var component = node.Value;
                    bool on_stack = false;
                    foreach (StackItem item in component)
                    {
                        if (item.StackReferences > 0 || item.ObjectReferences?.Values.Any(p => p.References > 0 && p.Item.OnStack) == true)
                        {
                            on_stack = true;
                            break;
                        }
                    }
                    if (on_stack)
                    {
                        foreach (StackItem item in component)
                            item.OnStack = true;
                        node = node.Next;
                    }
                    else
                    {
                        foreach (StackItem item in component)
                        {
                            tracked_items.Remove(item);
                            if (item is CompoundType compound)
                            {
                                references_count -= compound.SubItemsCount;
                                foreach (StackItem subitem in compound.SubItems)
                                {
                                    if (component.Contains(subitem)) continue;
                                    subitem.ObjectReferences!.TryRemove(compound, out _);
                                }
                            }
                            item.Cleanup();
                        }
                        var nodeToRemove = node;
                        node = node.Next;
                        cached_components.Remove(nodeToRemove);
                    }
                }
            }
            return references_count;
        }

        internal void RemoveReference(StackItem item, CompoundType parent)
        {
            references_count--;
            cached_components = null;
            if (item.ObjectReferences!.TryGetValue(parent, out var value))
            {
                value.References--;
            }
            if (item.StackReferences == 0)
                zero_referred.Add(item);
        }

        internal void RemoveStackReference(StackItem item)
        {
            references_count--;
            if (--item.StackReferences == 0)
                zero_referred.Add(item);
        }
    }
}
