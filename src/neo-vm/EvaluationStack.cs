using Neo.VM.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    public sealed class EvaluationStack : IReadOnlyCollection<StackItem>
    {
        private readonly List<StackItem> innerList = new List<StackItem>();
        private readonly ReferenceCounter referenceCounter;

        internal EvaluationStack(ReferenceCounter referenceCounter)
        {
            this.referenceCounter = referenceCounter;
        }

        public int Count => innerList.Count;

        internal void Clear()
        {
            foreach (StackItem item in innerList)
                referenceCounter.RemoveStackReference(item);
            innerList.Clear();
        }

        internal void CopyTo(EvaluationStack stack, int count = -1)
        {
            if (count == 0) return;
            if (count == -1)
                stack.innerList.AddRange(innerList);
            else
                stack.innerList.AddRange(innerList.Skip(innerList.Count - count));
        }

        public IEnumerator<StackItem> GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Insert(int index, StackItem item)
        {
            if (index > innerList.Count) throw new InvalidOperationException();
            innerList.Insert(innerList.Count - index, item);
            referenceCounter.AddStackReference(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackItem Peek(int index = 0)
        {
            if (index >= innerList.Count) throw new InvalidOperationException();
            if (index < 0)
            {
                index += innerList.Count;
                if (index < 0) throw new InvalidOperationException();
            }
            return innerList[innerList.Count - index - 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackItem Pop()
        {
            if (!TryPop(out StackItem item))
                throw new InvalidOperationException();
            return item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(StackItem item)
        {
            innerList.Add(item);
            referenceCounter.AddStackReference(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool Reverse(int n)
        {
            if (n < 0 || n > innerList.Count) return false;
            if (n <= 1) return true;
            innerList.Reverse(innerList.Count - n, n);
            return true;
        }

        public bool TryPeek<T>(out T item) where T : StackItem
        {
            if (innerList.Count == 0)
            {
                item = default;
                return false;
            }
            item = innerList[^1] as T;
            return item != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop<T>(out T item) where T : StackItem
        {
            return TryRemove(0, out item);
        }

        internal bool TryRemove<T>(int index, out T item) where T : StackItem
        {
            if (index >= innerList.Count)
            {
                item = default;
                return false;
            }
            if (index < 0)
            {
                index += innerList.Count;
                if (index < 0)
                {
                    item = default;
                    return false;
                }
            }
            index = innerList.Count - index - 1;
            item = innerList[index] as T;
            if (item is null) return false;
            innerList.RemoveAt(index);
            referenceCounter.RemoveStackReference(item);
            return true;
        }
    }
}
