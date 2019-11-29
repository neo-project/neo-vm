using Neo.VM.Types;
using System.Collections.Generic;

namespace Neo.VM
{
    public sealed class ReferenceCounter
    {
        private class Entry
        {
            public int StackReferences;
            public Dictionary<CompoundType, int> ObjectReferences;
        }

        private readonly Dictionary<CompoundType, Entry> counter = new Dictionary<CompoundType, Entry>(ReferenceEqualityComparer.Default);
        private readonly List<CompoundType> zero_referred = new List<CompoundType>();
        private int references_count = 0;

        public int Count => references_count;

        internal void AddReference(StackItem referred, CompoundType parent)
        {
            references_count++;
            if (!(referred is CompoundType compound)) return;
            if (!counter.TryGetValue(compound, out Entry tracing))
            {
                tracing = new Entry();
                counter.Add(compound, tracing);
            }
            if (tracing.ObjectReferences is null)
                tracing.ObjectReferences = new Dictionary<CompoundType, int>(ReferenceEqualityComparer.Default);
            if (tracing.ObjectReferences.TryGetValue(parent, out int count))
                count++;
            else
                count = 1;
            tracing.ObjectReferences[parent] = count;
        }

        internal void AddStackReference(StackItem referred)
        {
            references_count++;
            if (!(referred is CompoundType compound)) return;
            if (counter.TryGetValue(compound, out Entry entry))
                entry.StackReferences++;
            else
                counter.Add(compound, new Entry { StackReferences = 1 });
        }

        internal void CheckZeroReferred()
        {
            if (zero_referred.Count == 0) return;
            HashSet<CompoundType> toBeDestroyed = new HashSet<CompoundType>(ReferenceEqualityComparer.Default);
            foreach (CompoundType compound in zero_referred)
            {
                HashSet<CompoundType> toBeDestroyedInLoop = new HashSet<CompoundType>(ReferenceEqualityComparer.Default);
                Queue<CompoundType> toBeChecked = new Queue<CompoundType>();
                toBeChecked.Enqueue(compound);
                while (toBeChecked.Count > 0)
                {
                    CompoundType c = toBeChecked.Dequeue();
                    Entry entry = counter[c];
                    if (entry.StackReferences > 0)
                    {
                        toBeDestroyedInLoop.Clear();
                        break;
                    }
                    toBeDestroyedInLoop.Add(c);
                    if (entry.ObjectReferences is null) continue;
                    foreach (var pair in entry.ObjectReferences)
                        if (pair.Value > 0 && !toBeDestroyed.Contains(pair.Key) && !toBeDestroyedInLoop.Contains(pair.Key))
                            toBeChecked.Enqueue(pair.Key);
                }
                if (toBeDestroyedInLoop.Count > 0)
                    toBeDestroyed.UnionWith(toBeDestroyedInLoop);
            }
            foreach (CompoundType compound in toBeDestroyed)
            {
                counter.Remove(compound);
                references_count -= compound.ItemsCount;
            }
            zero_referred.Clear();
        }

        internal void RemoveReference(StackItem referred, CompoundType parent)
        {
            references_count--;
            if (!(referred is CompoundType compound)) return;
            Entry entry = counter[compound];
            entry.ObjectReferences[parent] -= 1;
            if (entry.StackReferences == 0)
                zero_referred.Add(compound);
        }

        internal void RemoveStackReference(StackItem referred)
        {
            references_count--;
            if (!(referred is CompoundType item_compound)) return;
            if (--counter[item_compound].StackReferences == 0)
                zero_referred.Add(item_compound);
        }
    }
}
