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
            public int StackReferences;
            public Dictionary<CompoundType, int>? ObjectReferences;
        }

        private readonly Dictionary<CompoundType, Entry> counter = new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<CompoundType> zero_referred = new(ReferenceEqualityComparer.Instance);
        private int references_count = 0;

        /// <summary>
        /// Indicates the number of this counter.
        /// </summary>
        public int Count => references_count;

        internal void AddReference(StackItem referred, CompoundType parent)
        {
            references_count++;
            if (referred is not CompoundType compound) return;
            if (!counter.TryGetValue(compound, out Entry? tracing))
            {
                tracing = new Entry();
                counter.Add(compound, tracing);
            }
            int count;
            if (tracing.ObjectReferences is null)
            {
                tracing.ObjectReferences = new Dictionary<CompoundType, int>(ReferenceEqualityComparer.Instance);
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

        internal void AddReferences(int count)
        {
            references_count += count;
        }

        internal void AddStackReference(StackItem referred)
        {
            references_count++;
            if (referred is not CompoundType compound) return;
            if (counter.TryGetValue(compound, out Entry? entry))
                entry.StackReferences++;
            else
                counter.Add(compound, new Entry { StackReferences = 1 });
            zero_referred.Remove(compound);
        }

        internal void AddZeroReferred(CompoundType item)
        {
            zero_referred.Add(item);
        }

        internal int CheckZeroReferred()
        {
            while (zero_referred.Count > 0)
            {
                HashSet<CompoundType> toBeDestroyed = new(ReferenceEqualityComparer.Instance);
                foreach (CompoundType compound in zero_referred)
                {
                    HashSet<CompoundType> toBeDestroyedInLoop = new(ReferenceEqualityComparer.Instance);
                    Queue<CompoundType> toBeChecked = new();
                    toBeChecked.Enqueue(compound);
                    while (toBeChecked.Count > 0)
                    {
                        CompoundType c = toBeChecked.Dequeue();
                        counter.TryGetValue(c, out Entry? entry);
                        if (entry?.StackReferences > 0)
                        {
                            toBeDestroyedInLoop.Clear();
                            break;
                        }
                        toBeDestroyedInLoop.Add(c);
                        if (entry?.ObjectReferences is null) continue;
                        foreach (var pair in entry.ObjectReferences)
                            if (pair.Value > 0 && !toBeDestroyed.Contains(pair.Key) && !toBeDestroyedInLoop.Contains(pair.Key))
                                toBeChecked.Enqueue(pair.Key);
                    }
                    if (toBeDestroyedInLoop.Count > 0)
                        toBeDestroyed.UnionWith(toBeDestroyedInLoop);
                }
                zero_referred.Clear();
                foreach (CompoundType compound in toBeDestroyed)
                {
                    counter.Remove(compound);
                    references_count -= compound.SubItemsCount;
                    foreach (CompoundType subitem in compound.SubItems.OfType<CompoundType>())
                    {
                        if (toBeDestroyed.Contains(subitem)) continue;
                        Entry entry = counter[subitem];
                        entry.ObjectReferences!.Remove(compound);
                        if (entry.StackReferences == 0)
                            zero_referred.Add(subitem);
                    }
                }
            }
            return references_count;
        }

        internal void RemoveReference(StackItem referred, CompoundType parent)
        {
            references_count--;
            if (referred is not CompoundType compound) return;
            Entry entry = counter[compound];
            entry.ObjectReferences![parent] -= 1;
            if (entry.StackReferences == 0)
                zero_referred.Add(compound);
        }

        internal void RemoveStackReference(StackItem referred)
        {
            references_count--;
            if (referred is not CompoundType item_compound) return;
            if (--counter[item_compound].StackReferences == 0)
                zero_referred.Add(item_compound);
        }
    }
}
