using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    public class RandomAccessStack<T> : IReadOnlyCollection<T>
    {
        private readonly List<T> list = new List<T>();

        public int Count => list.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            list.Clear();
        }

        public void CopyTo(RandomAccessStack<T> stack, int count = -1)
        {
            if (count == 0) return;
            if (count == -1)
                stack.list.AddRange(list);
            else
                stack.list.AddRange(list.Skip(list.Count - count));
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

        public void Insert(int index, T item)
        {
            if (index > list.Count) throw new InvalidOperationException();
            list.Insert(list.Count - index, item);
        }

        public T Peek(int index = 0)
        {
            if (index >= list.Count) throw new InvalidOperationException();
            if (index < 0)
            {
                index += list.Count;
                if (index < 0) throw new InvalidOperationException();
            }
            return list[(list.Count - index - 1)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop()
        {
            return Remove(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(T item)
        {
            list.Add(item);
        }

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
            return item;
        }

        public void Set(int index, T item)
        {
            if (index >= list.Count) throw new InvalidOperationException();
            if (index < 0)
            {
                index += list.Count;
                if (index < 0) throw new InvalidOperationException();
            }
            list[(list.Count - index - 1)] = item;
        }
    }
}