using System;
using System.Collections;
using System.Collections.Generic;

namespace Neo.VM
{
    public class RandomAccessStack<T> : IReadOnlyCollection<T>
    {
        private readonly List<T> list = new List<T>();

        public int Count => list.Count;

        public void Clear()
        {
            list.Clear();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Insert(int index, T item)
        {
            if (index > list.Count) throw new InvalidOperationException();
            list.Insert(list.Count - index, item);
        }

        public T Peek(int index = 0)
        {
            if (index >= list.Count) throw new InvalidOperationException();
            return list[list.Count - 1 - index];
        }

        public T Pop()
        {
            return Remove(0);
        }

        public void Push(T item)
        {
            list.Add(item);
        }

        public T Remove(int index)
        {
            if (index >= list.Count) throw new InvalidOperationException();
            T item = list[list.Count - index - 1];
            list.RemoveAt(list.Count - index - 1);
            return item;
        }

        public void Set(int index, T item)
        {
            if (index >= list.Count) throw new InvalidOperationException();
            list[list.Count - index - 1] = item;
        }
    }
}
