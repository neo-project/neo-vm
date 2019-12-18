using System.Collections;
using System.Collections.Generic;

namespace Neo.VM.Types
{
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
        internal override IEnumerable<StackItem> SubItems => _array;
        internal override int SubItemsCount => _array.Count;
        public override StackItemType Type => StackItemType.Array;

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

        public override void Clear()
        {
            if (ReferenceCounter != null)
                foreach (StackItem item in _array)
                    ReferenceCounter.RemoveReference(item, this);
            _array.Clear();
        }

        public override StackItem ConvertTo(StackItemType type)
        {
            if (Type == StackItemType.Array && type == StackItemType.Struct)
                return new Struct(ReferenceCounter, new List<StackItem>(_array));
            return base.ConvertTo(type);
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
