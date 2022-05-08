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
        private class Entry
        {
            public static readonly Entry Empty = new();
            public int StackReferences;
            public Dictionary<CompoundType, int>? ObjectReferences;
        }

        private readonly Dictionary<StackItem, Entry> counter = new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<StackItem> zero_referred = new(ReferenceEqualityComparer.Instance);
        private int references_count = 0;

        /// <summary>
        /// Indicates the number of this counter.
        /// </summary>
        public int Count => references_count;

        internal void AddReference(StackItem item, CompoundType parent)
        {
            references_count++;
            if (!counter.TryGetValue(item, out Entry? tracing))
            {
                tracing = new Entry();
                counter.Add(item, tracing);
            }
            int count;
            if (tracing.ObjectReferences is null)
            {
                tracing.ObjectReferences = new(ReferenceEqualityComparer.Instance);
                count = 1;
            }
            else
            {
                if (tracing.ObjectReferences.TryGetValue(parent, out count))
                    count++;
                else
                    count = 1;
            }
            tracing.ObjectReferences[parent] = count;
        }

        internal void AddStackReference(StackItem item, int count = 1)
        {
            references_count += count;
            if (counter.TryGetValue(item, out Entry? entry))
                entry.StackReferences += count;
            else
                counter.Add(item, new Entry { StackReferences = count });
            zero_referred.Remove(item);
        }

        internal void AddZeroReferred(StackItem item)
        {
            zero_referred.Add(item);
        }

        internal int CheckZeroReferred()
        {
            if (zero_referred.Count > 0)
            {
                HashSet<StackItem> items_on_stack = new(ReferenceEqualityComparer.Instance);
                var vertexsTable = counter.ToDictionary<KeyValuePair<StackItem, Entry>, StackItem, Vertex<(StackItem, Entry)>>(p => p.Key, p => new Vertex<(StackItem, Entry)>((p.Key, p.Value)), ReferenceEqualityComparer.Instance);
                foreach (StackItem item in zero_referred)
                    if (!vertexsTable.ContainsKey(item))
                        vertexsTable.Add(item, new Vertex<(StackItem, Entry)>((item, Entry.Empty)));
                zero_referred.Clear();
                foreach (var (_, vertex) in vertexsTable)
                {
                    var (_, entry) = vertex.Value;
                    if (entry.ObjectReferences is null) continue;
                    vertex.Successors = entry.ObjectReferences
                        .Where(p => p.Value > 0)
                        .Select(p => vertexsTable[p.Key])
                        .ToArray();
                }
                Tarjan<(StackItem Item, Entry Entry)> tarjan = new(vertexsTable.Values);
                var components = tarjan.Invoke();
                foreach (var component in components)
                {
                    bool on_stack = false;
                    foreach (var vertex in component)
                    {
                        var (_, entry) = vertex.Value;
                        if (entry.StackReferences > 0 || entry.ObjectReferences?.Any(p => p.Value > 0 && items_on_stack.Contains(p.Key)) == true)
                        {
                            on_stack = true;
                            break;
                        }
                    }
                    if (on_stack)
                    {
                        items_on_stack.UnionWith(component.Select(p => p.Value.Item));
                    }
                    else
                    {
                        HashSet<StackItem> toBeDestroyed = new(component.Select(p => p.Value.Item), ReferenceEqualityComparer.Instance);
                        foreach (var item in toBeDestroyed)
                        {
                            counter.Remove(item);
                            if (item is CompoundType compound)
                            {
                                references_count -= compound.SubItemsCount;
                                foreach (StackItem subitem in compound.SubItems)
                                {
                                    if (toBeDestroyed.Contains(subitem)) continue;
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
            Entry entry = counter[item];
            entry.ObjectReferences![parent] -= 1;
            if (entry.StackReferences == 0)
                zero_referred.Add(item);
        }

        internal void RemoveStackReference(StackItem item)
        {
            references_count--;
            if (--counter[item].StackReferences == 0)
                zero_referred.Add(item);
        }
    }
}
