using Neo.VM.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Neo.VM.Types
{
    public class Map : CompoundType, IReadOnlyDictionary<PrimitiveType, StackItem>
    {
        public const int MaxKeySize = 64;

        private readonly OrderedDictionary<PrimitiveType, StackItem> dictionary = new OrderedDictionary<PrimitiveType, StackItem>();

        public StackItem this[PrimitiveType key]
        {
            get
            {
                if (key.Size > MaxKeySize)
                    throw new ArgumentException($"MaxKeySize exceed: {key.Size}");
                return dictionary[key];
            }
            set
            {
                if (key.Size > MaxKeySize)
                    throw new ArgumentException($"MaxKeySize exceed: {key.Size}");
                if (ReferenceCounter != null)
                {
                    if (dictionary.TryGetValue(key, out StackItem old_value))
                        ReferenceCounter.RemoveReference(old_value, this);
                    else
                        ReferenceCounter.AddReference(key, this);
                    ReferenceCounter.AddReference(value, this);
                }
                dictionary[key] = value;
            }
        }

        public override int Count => dictionary.Count;
        public IEnumerable<PrimitiveType> Keys => dictionary.Keys;
        internal override IEnumerable<StackItem> SubItems => Keys.Concat(Values);
        internal override int SubItemsCount => dictionary.Count * 2;
        public override StackItemType Type => StackItemType.Map;
        public IEnumerable<StackItem> Values => dictionary.Values;

        public Map(ReferenceCounter referenceCounter = null)
            : base(referenceCounter)
        {
        }

        public override void Clear()
        {
            if (ReferenceCounter != null)
                foreach (var pair in dictionary)
                {
                    ReferenceCounter.RemoveReference(pair.Key, this);
                    ReferenceCounter.RemoveReference(pair.Value, this);
                }
            dictionary.Clear();
        }

        public bool ContainsKey(PrimitiveType key)
        {
            if (key.Size > MaxKeySize)
                throw new ArgumentException($"MaxKeySize exceed: {key.Size}");
            return dictionary.ContainsKey(key);
        }

        internal override StackItem DeepCopy(Dictionary<StackItem, StackItem> refMap)
        {
            if (refMap.TryGetValue(this, out StackItem mappedItem)) return mappedItem;
            Map result = new Map(ReferenceCounter);
            refMap.Add(this, result);
            foreach (var pair in dictionary)
                result[pair.Key] = pair.Value.DeepCopy(refMap);
            return result;
        }

        IEnumerator<KeyValuePair<PrimitiveType, StackItem>> IEnumerable<KeyValuePair<PrimitiveType, StackItem>>.GetEnumerator()
        {
            return ((IDictionary<PrimitiveType, StackItem>)dictionary).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary<PrimitiveType, StackItem>)dictionary).GetEnumerator();
        }

        public bool Remove(PrimitiveType key)
        {
            if (key.Size > MaxKeySize)
                throw new ArgumentException($"MaxKeySize exceed: {key.Size}");
            if (!dictionary.Remove(key, out StackItem old_value))
                return false;
            ReferenceCounter?.RemoveReference(key, this);
            ReferenceCounter?.RemoveReference(old_value, this);
            return true;
        }

        public bool TryGetValue(PrimitiveType key, out StackItem value)
        {
            if (key.Size > MaxKeySize)
                throw new ArgumentException($"MaxKeySize exceed: {key.Size}");
            return dictionary.TryGetValue(key, out value);
        }
    }
}
