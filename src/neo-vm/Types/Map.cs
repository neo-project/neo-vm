using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Neo.VM.Types
{
    [DebuggerDisplay("type=Map, count={Count}")]
    public class Map : StackItem, ICollection, IDictionary<StackItem, StackItem>
    {
        private readonly Dictionary<StackItem, StackItem> dictionary;

        public StackItem this[StackItem key]
        {
            get => dictionary[key];
            set => dictionary[key] = value;
        }

        public ICollection<StackItem> Keys => dictionary.Keys;
        public ICollection<StackItem> Values => dictionary.Values;
        public int Count => dictionary.Count;
        public bool IsReadOnly => false;

        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => dictionary;

        public Map() : this(new Dictionary<StackItem, StackItem>()) { }

        public Map(Dictionary<StackItem, StackItem> value)
        {
            dictionary = value;
        }

        public void Add(StackItem key, StackItem value) => dictionary.Add(key, value);

        void ICollection<KeyValuePair<StackItem, StackItem>>.Add(KeyValuePair<StackItem, StackItem> item) => dictionary.Add(item.Key, item.Value);

        public void Clear() => dictionary.Clear();

        bool ICollection<KeyValuePair<StackItem, StackItem>>.Contains(KeyValuePair<StackItem, StackItem> item) => dictionary.ContainsKey(item.Key);

        public bool ContainsKey(StackItem key) => dictionary.ContainsKey(key);

        void ICollection<KeyValuePair<StackItem, StackItem>>.CopyTo(KeyValuePair<StackItem, StackItem>[] array, int arrayIndex)
        {
            foreach (KeyValuePair<StackItem, StackItem> item in dictionary)
                array[arrayIndex++] = item;
        }

        void ICollection.CopyTo(System.Array array, int index)
        {
            foreach (KeyValuePair<StackItem, StackItem> item in dictionary)
                array.SetValue(item, index++);
        }

        public override bool Equals(StackItem other) => ReferenceEquals(this, other);

        public override bool GetBoolean() => true;

        public override byte[] GetByteArray() => throw new NotSupportedException();

        IEnumerator<KeyValuePair<StackItem, StackItem>> IEnumerable<KeyValuePair<StackItem, StackItem>>.GetEnumerator() => dictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => dictionary.GetEnumerator();

        public bool Remove(StackItem key) => dictionary.Remove(key);

        bool ICollection<KeyValuePair<StackItem, StackItem>>.Remove(KeyValuePair<StackItem, StackItem> item) => dictionary.Remove(item.Key);

        public bool TryGetValue(StackItem key, out StackItem value) => dictionary.TryGetValue(key, out value);
    }
}
