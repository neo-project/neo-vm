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

        private class ObjectReferenceEntry
        {
            public ReferenceEntry Entry;
            public int References;
            public ObjectReferenceEntry(ReferenceEntry entry) => Entry = entry;
        }

        private class ReferenceEntry : Vertex<ReferenceEntry>
        {
            public StackItem Item;
            public int StackReferences;
            public Dictionary<CompoundType, ObjectReferenceEntry>? ObjectReferences;
            public ReferenceEntry(StackItem item) => Item = item;
            protected internal override IEnumerable<ReferenceEntry> Successors => ObjectReferences?.Values.Where(p => p.References > 0).Select(p => p.Entry) ?? System.Array.Empty<ReferenceEntry>();
        }

        private readonly Dictionary<StackItem, ReferenceEntry> counter = new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<StackItem> zero_referred = new(ReferenceEqualityComparer.Instance);
        private int references_count = 0;

        /// <summary>
        /// Indicates the number of this counter.
        /// </summary>
        public int Count => references_count;

        internal void AddReference(StackItem item, CompoundType parent)
        {
            references_count++;
            if (TrackCompoundTypeOnly && item is not CompoundType) return;
            if (!counter.TryGetValue(item, out ReferenceEntry? tracing))
            {
                tracing = new ReferenceEntry(item);
                counter.Add(item, tracing);
            }
            tracing.ObjectReferences ??= new(ReferenceEqualityComparer.Instance);
            if (!tracing.ObjectReferences.TryGetValue(parent, out ObjectReferenceEntry? objEntry))
            {
                objEntry = new ObjectReferenceEntry(counter[parent]);
                tracing.ObjectReferences.Add(parent, objEntry);
            }
            objEntry.References++;
        }

        internal void AddStackReference(StackItem item, int count = 1)
        {
            references_count += count;
            if (TrackCompoundTypeOnly && item is not CompoundType) return;
            if (counter.TryGetValue(item, out ReferenceEntry? entry))
                entry.StackReferences += count;
            else
                counter.Add(item, new ReferenceEntry(item) { StackReferences = count });
            zero_referred.Remove(item);
        }

        internal void AddZeroReferred(StackItem item)
        {
            zero_referred.Add(item);
            if (TrackCompoundTypeOnly && item is not CompoundType) return;
            if (!counter.ContainsKey(item))
                counter.Add(item, new ReferenceEntry(item));
        }

        internal int CheckZeroReferred()
        {
            if (zero_referred.Count > 0)
            {
                HashSet<StackItem> items_on_stack = new(ReferenceEqualityComparer.Instance);
                zero_referred.Clear();
                foreach (ReferenceEntry entry in counter.Values)
                    entry.Reset();
                Tarjan<ReferenceEntry> tarjan = new(counter.Values.Where(p => p.StackReferences == 0));
                var components = tarjan.Invoke();
                foreach (var component in components)
                {
                    bool on_stack = false;
                    foreach (var vertex in component)
                    {
                        if (vertex.StackReferences > 0 || vertex.ObjectReferences?.Values.Any(p => p.References > 0 && items_on_stack.Contains(p.Entry.Item)) == true)
                        {
                            on_stack = true;
                            break;
                        }
                    }
                    if (on_stack)
                    {
                        items_on_stack.UnionWith(component.Select(p => p.Item));
                    }
                    else
                    {
                        HashSet<StackItem> toBeDestroyed = new(component.Select(p => p.Item), ReferenceEqualityComparer.Instance);
                        foreach (var item in toBeDestroyed)
                        {
                            counter.Remove(item);
                            if (item is CompoundType compound)
                            {
                                references_count -= compound.SubItemsCount;
                                foreach (StackItem subitem in compound.SubItems)
                                {
                                    if (toBeDestroyed.Contains(subitem)) continue;
                                    if (TrackCompoundTypeOnly && subitem is not CompoundType) continue;
                                    counter[subitem].ObjectReferences!.Remove(compound);
                                }
                            }
                            // Todo: We can do StackItem cleanup here in the future.
                        }
                    }
                }
            }
            return references_count;
        }

        internal void RemoveReference(StackItem item, CompoundType parent)
        {
            references_count--;
            if (TrackCompoundTypeOnly && item is not CompoundType) return;
            ReferenceEntry entry = counter[item];
            entry.ObjectReferences![parent].References--;
            if (entry.StackReferences == 0)
                zero_referred.Add(item);
        }

        internal void RemoveStackReference(StackItem item)
        {
            references_count--;
            if (TrackCompoundTypeOnly && item is not CompoundType) return;
            if (--counter[item].StackReferences == 0)
                zero_referred.Add(item);
        }
    }
}
