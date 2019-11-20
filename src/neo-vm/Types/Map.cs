using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Count={Count}")]
    public class Map : CompoundType, IDictionary<PrimitiveType, StackItem>
    {
        private readonly Dictionary<PrimitiveType, StackItem> dictionary;

        public StackItem this[PrimitiveType key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => dictionary[key];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => dictionary[key] = value;
        }

        public ICollection<PrimitiveType> Keys => dictionary.Keys;
        public ICollection<StackItem> Values => dictionary.Values;
        public override int Count => dictionary.Count;
        public bool IsReadOnly => false;

        public Map() : this(new Dictionary<PrimitiveType, StackItem>()) { }

        public Map(Dictionary<PrimitiveType, StackItem> value)
        {
            dictionary = value;
        }

        public void Add(PrimitiveType key, StackItem value)
        {
            dictionary.Add(key, value);
        }

        void ICollection<KeyValuePair<PrimitiveType, StackItem>>.Add(KeyValuePair<PrimitiveType, StackItem> item)
        {
            dictionary.Add(item.Key, item.Value);
        }

        public override void Clear()
        {
            dictionary.Clear();
        }

        bool ICollection<KeyValuePair<PrimitiveType, StackItem>>.Contains(KeyValuePair<PrimitiveType, StackItem> item)
        {
            return dictionary.ContainsKey(item.Key);
        }

        public bool ContainsKey(PrimitiveType key)
        {
            return dictionary.ContainsKey(key);
        }

        void ICollection<KeyValuePair<PrimitiveType, StackItem>>.CopyTo(KeyValuePair<PrimitiveType, StackItem>[] array, int arrayIndex)
        {
            foreach (KeyValuePair<PrimitiveType, StackItem> item in dictionary)
                array[arrayIndex++] = item;
        }

        IEnumerator<KeyValuePair<PrimitiveType, StackItem>> IEnumerable<KeyValuePair<PrimitiveType, StackItem>>.GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        public bool Remove(PrimitiveType key)
        {
            return dictionary.Remove(key);
        }

        bool ICollection<KeyValuePair<PrimitiveType, StackItem>>.Remove(KeyValuePair<PrimitiveType, StackItem> item)
        {
            return dictionary.Remove(item.Key);
        }

        public bool TryGetValue(PrimitiveType key, out StackItem value)
        {
            return dictionary.TryGetValue(key, out value);
        }
    }
}
