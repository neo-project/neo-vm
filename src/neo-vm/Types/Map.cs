using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Count={Count}")]
    public class Map : CompoundType, IDictionary<PrimitiveType, StackItem>
    {
        private readonly ReservedMemory _memory;
        private readonly Dictionary<PrimitiveType, StackItem> dictionary;

        public StackItem this[PrimitiveType key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => dictionary[key];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (dictionary.TryGetValue(key, out var before))
                {
                    _memory.Remove(before);
                }
                else
                {
                    _memory.Add(key);
                }
                _memory.Add(value);
                dictionary[key] = value;
            }
        }

        public ICollection<PrimitiveType> Keys => dictionary.Keys;
        public ICollection<StackItem> Values => dictionary.Values;
        public override int Count => dictionary.Count;
        public bool IsReadOnly => false;

        public Map(ReservedMemory memory)
        {
            _memory = memory;
            dictionary = new Dictionary<PrimitiveType, StackItem>();
        }

        public Map(ReservedMemory memory, Dictionary<PrimitiveType, StackItem> value)
        {
            _memory = memory;
            dictionary = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(PrimitiveType key, StackItem value)
        {
            _memory.Add(key);
            _memory.Add(value);

            dictionary.Add(key, value);
        }

        void ICollection<KeyValuePair<PrimitiveType, StackItem>>.Add(KeyValuePair<PrimitiveType, StackItem> item)
        {
            Add(item.Key, item.Value);
        }

        public override void Clear()
        {
            _memory.RemoveRange(Keys);
            _memory.RemoveRange(Values);

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
            if (dictionary.TryGetValue(key, out var value))
            {
                _memory.Remove(key);
                _memory.Remove(value);
                dictionary.Remove(key);
                return true;
            }

            return false;
        }

        bool ICollection<KeyValuePair<PrimitiveType, StackItem>>.Remove(KeyValuePair<PrimitiveType, StackItem> item)
        {
            return Remove(item.Key);
        }

        public bool TryGetValue(PrimitiveType key, out StackItem value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        public override void OnAddMemory(ReservedMemory memory)
        {
            memory.AllocateMemory();

            foreach (var key in this)
            {
                memory.Add(key.Key);
                memory.Add(key.Value);
            }
        }

        public override void OnRemoveFromMemory(ReservedMemory memory)
        {
            memory.FreeMemory();

            foreach (var key in this)
            {
                memory.Remove(key.Key);
                memory.Remove(key.Value);
            }
        }
    }
}
