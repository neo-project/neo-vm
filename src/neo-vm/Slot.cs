using Neo.VM.Types;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Neo.VM
{
    public class Slot : IReadOnlyList<StackItem>
    {
        private readonly ReferenceCounter referenceCounter;
        private readonly StackItem[] items;

        public StackItem this[int index]
        {
            get
            {
                return items[index];
            }
            internal set
            {
                referenceCounter.RemoveStackReference(items[index]);
                items[index] = value;
                referenceCounter.AddStackReference(value);
            }
        }

        public int Count => items.Length;

        public Slot(StackItem[] items, ReferenceCounter referenceCounter)
        {
            this.referenceCounter = referenceCounter;
            this.items = items;
            foreach (StackItem item in items)
                referenceCounter.AddStackReference(item);
        }

        public Slot(int count, ReferenceCounter referenceCounter)
            : this(Enumerable.Repeat(StackItem.Null, count).ToArray(), referenceCounter)
        {
        }

        internal void ClearReferences()
        {
            foreach (StackItem item in items)
                referenceCounter.RemoveStackReference(item);
        }

        IEnumerator<StackItem> IEnumerable<StackItem>.GetEnumerator()
        {
            foreach (StackItem item in items) yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }
    }
}
