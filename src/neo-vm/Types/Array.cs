using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Count={Count}")]
    public class Array : CompoundType, IList<StackItem>
    {
        protected readonly List<StackItem> _array;

        public StackItem this[int index]
        {
            get => _array[index];
            set => _array[index] = value;
        }

        public override int Count => _array.Count;
        public bool IsReadOnly => false;

        public Array() : this(new List<StackItem>()) { }

        public Array(IEnumerable<StackItem> value)
        {
            _array = value as List<StackItem> ?? value.ToList();
        }

        public void Add(StackItem item)
        {
            _array.Add(item);
        }

        public override void Clear()
        {
            _array.Clear();
        }

        public bool Contains(StackItem item)
        {
            return _array.Contains(item);
        }

        void ICollection<StackItem>.CopyTo(StackItem[] array, int arrayIndex)
        {
            _array.CopyTo(array, arrayIndex);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<StackItem> GetEnumerator()
        {
            return _array.GetEnumerator();
        }

        int IList<StackItem>.IndexOf(StackItem item)
        {
            return _array.IndexOf(item);
        }

        public void Insert(int index, StackItem item)
        {
            _array.Insert(index, item);
        }

        bool ICollection<StackItem>.Remove(StackItem item)
        {
            return _array.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _array.RemoveAt(index);
        }

        public void Reverse()
        {
            _array.Reverse();
        }
    }
}
