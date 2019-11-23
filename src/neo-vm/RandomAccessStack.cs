using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    [DebuggerDisplay("Count={Count}")]
    public class RandomAccessStack<T> : IReadOnlyCollection<T>
        where T : IMemoryItem
    {
        private readonly List<T> list = new List<T>();
        private readonly ReservedMemory _memory;

        public RandomAccessStack(ReservedMemory memory)
        {
            _memory = memory;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return list.Count; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _memory.RemoveRange(list);
            list.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(RandomAccessStack<T> stack, int count = -1)
        {
            if (count == 0) return;
            if (count == -1)
            {
                stack._memory.AddRange(list);
                stack.list.AddRange(list);
            }
            else
            {
                stack._memory.AddRange(list.Skip(list.Count - count));
                stack.list.AddRange(list.Skip(list.Count - count));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(int index, T item)
        {
            if (index > list.Count) throw new InvalidOperationException();
            _memory.Add(item);
            list.Insert(list.Count - index, item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek(int index = 0)
        {
            if (index >= list.Count) throw new InvalidOperationException();
            if (index < 0)
            {
                index += list.Count;
                if (index < 0) throw new InvalidOperationException();
            }
            return list[list.Count - index - 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop()
        {
            return Remove(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out T item)
        {
            var index = list.Count - 1;

            if (index >= 0)
            {
                item = list[index];
                list.RemoveAt(index);
                _memory.Remove(item);
                return true;
            }

            item = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop<TItem>(out TItem item) where TItem : T
        {
            var index = list.Count - 1;

            if (index >= 0 && list[index] is TItem i)
            {
                item = i;
                list.RemoveAt(index);
                _memory.Remove(i);
                return true;
            }

            item = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(T item)
        {
            _memory.Add(item);
            list.Add(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Remove(int index)
        {
            if (index >= list.Count) throw new InvalidOperationException();
            if (index < 0)
            {
                index += list.Count;
                if (index < 0) throw new InvalidOperationException();
            }
            index = list.Count - index - 1;
            T item = list[index];
            list.RemoveAt(index);
            _memory.Remove(item);
            return item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index, T item)
        {
            if (index >= list.Count) throw new InvalidOperationException();
            if (index < 0)
            {
                index += list.Count;
                if (index < 0) throw new InvalidOperationException();
            }
            _memory.Remove(list[list.Count - index - 1]);
            _memory.Add(item);
            list[list.Count - index - 1] = item;
        }
    }
}
