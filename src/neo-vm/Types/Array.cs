using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Neo.VM.Types
{
    public class Array : StackItem, IList<StackItem>
    {
        protected readonly List<StackItem> _array;

        public StackItem this[int index]
        {
            get => _array[index];
            set => _array[index] = value;
        }

        public int Count => _array.Count;
        public bool IsReadOnly => false;

        public Array() : this(new List<StackItem>()) { }

        public Array(IEnumerable<StackItem> value)
        {
            this._array = value as List<StackItem> ?? value.ToList();
        }

        public void Add(StackItem item)
        {
            _array.Add(item);
        }

        public void Clear()
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

        public override bool Equals(StackItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            Array a = other as Array;
            if (a == null)
                return false;
            else
                return _array.SequenceEqual(a._array);
        }

        public override bool GetBoolean()
        {
            return _array.Count > 0;
        }

        public override byte[] GetByteArray()
        {
            throw new NotSupportedException();
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
