using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Count={Count}")]
    public class Array : CompoundType, IList<StackItem>
    {
        private readonly ReservedMemory _memory;
        protected readonly List<StackItem> _array;

        public StackItem this[int index]
        {
            get => _array[index];
            set
            {
                _memory.Remove(_array[index]);
                _memory.Add(value);
                _array[index] = value;
            }
        }

        public override int Count => _array.Count;
        public bool IsReadOnly => false;

        public Array(ReservedMemory memory)
        {
            _memory = memory;
            _array = new List<StackItem>();
        }

        public Array(ReservedMemory memory, IEnumerable<StackItem> value)
        {
            _memory = memory;
            _array = value as List<StackItem> ?? value.ToList();
        }

        public override void OnAddMemory(ReservedMemory memory)
        {
            memory.AllocateMemory();
            memory.AddRange(_array);
        }

        public override void OnRemoveFromMemory(ReservedMemory memory)
        {
            memory.FreeMemory();
            memory.RemoveRange(_array);
        }

        public void Add(StackItem item)
        {
            _memory.Add(item);
            _array.Add(item);
        }

        public override void Clear()
        {
            _array.Clear();
            _memory.RemoveRange(_array);
        }

        public bool Contains(StackItem item)
        {
            return _array.Contains(item);
        }

        void ICollection<StackItem>.CopyTo(StackItem[] array, int arrayIndex)
        {
            _array.CopyTo(array, arrayIndex);
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
            _memory.Add(item);
            _array.Insert(index, item);
        }

        bool ICollection<StackItem>.Remove(StackItem item)
        {
            if (_array.Remove(item))
            {
                _memory.Remove(item);
                return true;
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            _memory.Remove(_array[index]);
            _array.RemoveAt(index);
        }

        public void Reverse()
        {
            _array.Reverse();
        }
    }
}
