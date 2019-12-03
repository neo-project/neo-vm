using Neo.VM.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    public sealed class EvaluationStack : IReadOnlyCollection<StackItem>
    {
        private readonly RandomAccessStack<StackItem> innerStack = new RandomAccessStack<StackItem>();
        private readonly ReferenceCounter referenceCounter;

        internal EvaluationStack(ReferenceCounter referenceCounter)
        {
            this.referenceCounter = referenceCounter;
        }

        public int Count => innerStack.Count;

        internal void Clear()
        {
            foreach (StackItem item in innerStack)
                referenceCounter.RemoveStackReference(item);
            innerStack.Clear();
        }

        internal void CopyTo(EvaluationStack stack, int count = -1)
        {
            innerStack.CopyTo(stack.innerStack, count);
        }

        public IEnumerator<StackItem> GetEnumerator()
        {
            return innerStack.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return innerStack.GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Insert(int index, StackItem item)
        {
            innerStack.Insert(index, item);
            referenceCounter.AddStackReference(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackItem Peek(int index = 0)
        {
            return innerStack.Peek(index);
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
            innerStack.Push(item);
            referenceCounter.AddStackReference(item);
        }

        internal void Set(int index, StackItem item)
        {
            StackItem old_item = innerStack.Peek(index);
            referenceCounter.RemoveStackReference(old_item);
            innerStack.Set(index, item);
            referenceCounter.AddStackReference(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop<T>(out T item) where T : StackItem
        {
            return TryRemove(0, out item);
        }

        internal bool TryRemove<T>(int index, out T item) where T : StackItem
        {
            if (!innerStack.TryRemove(index, out StackItem stackItem))
            {
                item = null;
                return false;
            }
            referenceCounter.RemoveStackReference(stackItem);
            item = stackItem as T;
            return item != null;
        }
    }
}
