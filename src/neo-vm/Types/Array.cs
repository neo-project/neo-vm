using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Count={Count}")]
    public class Array : CompoundType, IReadOnlyList<StackItem>
    {
        protected readonly List<StackItem> _array;

        public StackItem this[int index]
        {
            get
            {
                return _array[index];
            }
            set
            {
                ReferenceCounter?.RemoveReference(_array[index], this);
                _array[index] = value;
                ReferenceCounter?.AddReference(value, this);
            }
        }

        public override int Count => _array.Count;
        public override int ItemsCount => _array.Count;

        public Array(IEnumerable<StackItem> value = null)
            : this(null, value)
        {
        }

        public Array(ReferenceCounter referenceCounter, IEnumerable<StackItem> value = null)
            : base(referenceCounter)
        {
            _array = value switch
            {
                null => new List<StackItem>(),
                List<StackItem> list => list,
                _ => new List<StackItem>(value)
            };
            if (referenceCounter != null)
                foreach (StackItem item in _array)
                    referenceCounter.AddReference(item, this);
        }

        public void Add(StackItem item)
        {
            _array.Add(item);
            ReferenceCounter?.AddReference(item, this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<StackItem> GetEnumerator()
        {
            return _array.GetEnumerator();
        }

        public void RemoveAt(int index)
        {
            ReferenceCounter?.RemoveReference(_array[index], this);
            _array.RemoveAt(index);
        }

        public void Reverse()
        {
            _array.Reverse();
        }
    }
}
