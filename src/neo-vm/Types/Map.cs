using Neo.VM.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Neo.VM.Types
{
    /// <summary>
    /// Represents an ordered collection of key-value pairs in the VM.
    /// </summary>
    public class Map : CompoundType, IReadOnlyDictionary<PrimitiveType, StackItem>
    {
        /// <summary>
        /// Indicates the maximum size of keys in bytes.
        /// </summary>
        public const int MaxKeySize = 64;

        private readonly OrderedDictionary<PrimitiveType, StackItem> dictionary = new OrderedDictionary<PrimitiveType, StackItem>();

        /// <summary>
        /// Gets or sets the element that has the specified key in the map.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns>The element that has the specified key in the map.</returns>
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

        /// <summary>
        /// Gets an enumerable collection that contains the keys in the map.
        /// </summary>
        public IEnumerable<PrimitiveType> Keys => dictionary.Keys;

        internal override IEnumerable<StackItem> SubItems => Keys.Concat(Values);

        internal override int SubItemsCount => dictionary.Count * 2;

        public override StackItemType Type => StackItemType.Map;

        /// <summary>
        /// Gets an enumerable collection that contains the values in the map.
        /// </summary>
        public IEnumerable<StackItem> Values => dictionary.Values;

        /// <summary>
        /// Create a new map with the specified reference counter.
        /// </summary>
        /// <param name="referenceCounter">The reference counter to be used.</param>
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

        /// <summary>
        /// Determines whether the map contains an element that has the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns>
        /// <see langword="true" /> if the map contains an element that has the specified key;
        /// otherwise, <see langword="false" />.
        /// </returns>
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

        /// <summary>
        /// Removes the element with the specified key from the map.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>
        /// <see langword="true" /> if the element is successfully removed;
        /// otherwise, <see langword="false"/>.
        /// This method also returns <see langword="false"/> if <paramref name="key"/> was not found in the original map.
        /// </returns>
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

        /// <summary>
        /// Gets the value that is associated with the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <param name="value">
        /// When this method returns, the value associated with the specified key, if the key is found;
        /// otherwise, <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <see langword="true" /> if the map contains an element that has the specified key;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(PrimitiveType key, out StackItem value)
        {
            if (key.Size > MaxKeySize)
                throw new ArgumentException($"MaxKeySize exceed: {key.Size}");
            return dictionary.TryGetValue(key, out value);
        }
    }
}
