﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Neo.VM.Types
{
    [DebuggerDisplay("type=Array, count={Count}")]
    public class Array : StackItem, ICollection, IList<StackItem>
    {
        protected readonly List<StackItem> _array;

        public StackItem this[int index]
        {
            get => _array[index];
            set => _array[index] = value;
        }

        public int Count => _array.Count;
        public bool IsReadOnly => false;

        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => _array;

        public Array() : this(new List<StackItem>()) { }

        public Array(IEnumerable<StackItem> value)
        {
            _array = value as List<StackItem> ?? value.ToList();
        }

        public void Add(StackItem item) => _array.Add(item);

        public void Clear() => _array.Clear();

        public bool Contains(StackItem item) => _array.Contains(item);

        void ICollection<StackItem>.CopyTo(StackItem[] array, int arrayIndex)
        {
            _array.CopyTo(array, arrayIndex);
        }

        void ICollection.CopyTo(System.Array array, int index)
        {
            foreach (StackItem item in _array)
                array.SetValue(item, index++);
        }

        public override bool Equals(StackItem other) => ReferenceEquals(this, other);

        public override bool GetBoolean() => true;

        public override byte[] GetByteArray() => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<StackItem> GetEnumerator() => _array.GetEnumerator();

        int IList<StackItem>.IndexOf(StackItem item) => _array.IndexOf(item);

        public void Insert(int index, StackItem item) => _array.Insert(index, item);

        bool ICollection<StackItem>.Remove(StackItem item) => _array.Remove(item);

        public void RemoveAt(int index) => _array.RemoveAt(index);

        public void Reverse() => _array.Reverse();
    }
}
