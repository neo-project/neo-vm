using System;
using System.Collections;
using System.Collections.Generic;

namespace AntShares.VM
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

        public T Peek(int index = 0)
        {
            if (index >= list.Count) throw new InvalidOperationException();
            return list[list.Count - 1 - index];
        }

        public T Pop()
        {
            if (list.Count == 0) throw new InvalidOperationException();
            T item = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return item;
        }

        public void Push(T item)
        {
            list.Add(item);
        }
    }
}
