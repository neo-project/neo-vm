using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

        public void CopyTo(RandomAccessStack<T> stack, int count = -1)
        {
            if (count == 0) return;
            if (count == -1)
                stack.list.AddRange(list);
            else
                stack.list.AddRange(list.Skip(list.Count - count));
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
            if (index < 0) index += list.Count;
            if (index < 0) throw new InvalidOperationException();
            index = list.Count - index - 1;
            return list[index];
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
            if (index < 0) index += list.Count;
            if (index < 0) throw new InvalidOperationException();
            index = list.Count - index - 1;
            T item = list[index];
            list.RemoveAt(index);
            return item;
        }

        public void Set(int index, T item)
        {
            if (index >= list.Count) throw new InvalidOperationException();
            if (index < 0) index += list.Count;
            if (index < 0) throw new InvalidOperationException();
            index = list.Count - index - 1;
            list[index] = item;
        }
    }
}
