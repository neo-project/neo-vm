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
using System.Collections.Generic;
using System.Linq;

namespace Neo.VM
{
    /// <summary>
    /// Used for reference counting of objects in the VM.
    /// </summary>
    public sealed class ReferenceCounter
    {
        private const bool TrackCompoundTypeOnly = true;

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
            if (TrackCompoundTypeOnly && item is not CompoundType) return;
            cached_components = null;
            tracked_items.Add(item);
            item.ObjectReferences ??= new(ReferenceEqualityComparer.Instance);
            if (!item.ObjectReferences.TryGetValue(parent, out var pEntry))
            {
                pEntry = new(parent);
                item.ObjectReferences.Add(parent, pEntry);
            }
            pEntry.References++;
        }

        internal void AddStackReference(StackItem item, int count = 1)
        {
            references_count += count;
            if (TrackCompoundTypeOnly && item is not CompoundType) return;
            tracked_items.Add(item);
            item.StackReferences += count;
            zero_referred.Remove(item);
        }

        internal void AddZeroReferred(StackItem item)
        {
            zero_referred.Add(item);
            if (TrackCompoundTypeOnly && item is not CompoundType) return;
            cached_components?.AddLast(new HashSet<StackItem>(ReferenceEqualityComparer.Instance) { item });
            tracked_items.Add(item);
        }

        internal int CheckZeroReferred()
        {
            if (zero_referred.Count > 0)
            {
                HashSet<StackItem> items_on_stack = new(ReferenceEqualityComparer.Instance);
                zero_referred.Clear();
                if (cached_components is null)
                {
                    foreach (IVertex<StackItem> vertex in tracked_items)
                        vertex.Reset();
                    //Tarjan<StackItem> tarjan = new(tracked_items.Where(p => p.StackReferences == 0));
                    Tarjan<StackItem> tarjan = new(tracked_items);
                    cached_components = tarjan.Invoke();
                }
                for (var node = cached_components.First; node != null;)
                {
                    var component = node.Value;
                    bool on_stack = false;
                    foreach (StackItem item in component)
                    {
                        if (item.StackReferences > 0 || item.ObjectReferences?.Values.Any(p => p.References > 0 && items_on_stack.Contains(p.Item)) == true)
                        {
                            on_stack = true;
                            break;
                        }
                    }
                    if (on_stack)
                    {
                        items_on_stack.UnionWith(component);
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
                                    if (TrackCompoundTypeOnly && subitem is not CompoundType) continue;
                                    subitem.ObjectReferences!.Remove(compound);
                                }
                            }
                            // Todo: We can do StackItem cleanup here in the future.
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
            if (TrackCompoundTypeOnly && item is not CompoundType) return;
            cached_components = null;
            item.ObjectReferences![parent].References--;
            if (item.StackReferences == 0)
                zero_referred.Add(item);
        }

        internal void RemoveStackReference(StackItem item)
        {
            references_count--;
            if (TrackCompoundTypeOnly && item is not CompoundType) return;
            if (--item.StackReferences == 0)
                zero_referred.Add(item);
        }
    }
}
