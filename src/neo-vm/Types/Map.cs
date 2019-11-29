using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Count={Count}")]
    public class Map : CompoundType, IReadOnlyDictionary<PrimitiveType, StackItem>
    {
        private readonly Dictionary<PrimitiveType, StackItem> dictionary;

        public StackItem this[PrimitiveType key]
        {
            get
            {
                return dictionary[key];
            }
            set
            {
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

        public IEnumerable<PrimitiveType> Keys => dictionary.Keys;
        public IEnumerable<StackItem> Values => dictionary.Values;
        public override int Count => dictionary.Count;

        public Map(Dictionary<PrimitiveType, StackItem> value = null)
            : this(null, value)
        {
        }

        public Map(ReferenceCounter referenceCounter, Dictionary<PrimitiveType, StackItem> value = null)
            : base(referenceCounter)
        {
            dictionary = value ?? new Dictionary<PrimitiveType, StackItem>();
        }

        public bool ContainsKey(PrimitiveType key)
        {
            return dictionary.ContainsKey(key);
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
            if (!dictionary.Remove(key, out StackItem old_value))
                return false;
            ReferenceCounter?.RemoveReference(key, this);
            ReferenceCounter?.RemoveReference(old_value, this);
            return true;
        }

        public bool TryGetValue(PrimitiveType key, out StackItem value)
        {
            return dictionary.TryGetValue(key, out value);
        }
    }
}
