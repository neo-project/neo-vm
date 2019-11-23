using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;

namespace Neo.Test
{
    [TestClass]
    public class UtRandomAccessStack
    {
        class Entry : IMemoryItem
        {
            public int Value;

            public int GetMemoryHashCode() => GetHashCode();

            public void OnAddMemory(ReservedMemory memory) { }

            public void OnRemoveFromMemory(ReservedMemory memory) { }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator int(Entry value)
            {
                return value.Value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Entry(int value)
            {
                return new Entry() { Value = value };
            }
        }

        RandomAccessStack<Entry> CreateOrderedStack(int count)
        {
            var check = new int[count];
            var stack = new RandomAccessStack<Entry>(new ReservedMemory());

            for (int x = 1; x <= count; x++)
            {
                stack.Push(new Entry() { Value = x });
                check[x - 1] = x;
            }

            Assert.AreEqual(count, stack.Count);
            CollectionAssert.AreEqual(check, stack.Select(u => u.Value).ToArray());

            return stack;
        }

        public IEnumerable GetEnumerable(IEnumerator enumerator)
        {
            while (enumerator.MoveNext()) yield return enumerator.Current;
        }

        [TestMethod]
        public void TestClear()
        {
            var stack = CreateOrderedStack(3);
            stack.Clear();
            Assert.AreEqual(0, stack.Count);
        }

        [TestMethod]
        public void TestCopyTo()
        {
            var stack = CreateOrderedStack(3);
            var copy = new RandomAccessStack<Entry>(new ReservedMemory());

            stack.CopyTo(copy, 0);

            Assert.AreEqual(3, stack.Count);
            Assert.AreEqual(0, copy.Count);
            CollectionAssert.AreEqual(new int[] { 1, 2, 3 }, ((IEnumerable<Entry>)stack).Select(u => u.Value).ToArray());

            stack.CopyTo(copy, -1);

            Assert.AreEqual(3, stack.Count);
            Assert.AreEqual(3, copy.Count);
            CollectionAssert.AreEqual(new int[] { 1, 2, 3 }, ((IEnumerable<Entry>)stack).Select(u => u.Value).ToArray());

            // Test IEnumerable

            var enumerable = (IEnumerable)copy;
            var enumerator = enumerable.GetEnumerator();

            CollectionAssert.AreEqual(new int[] { 1, 2, 3 }, GetEnumerable(enumerator).Cast<Entry>().Select(u => u.Value).ToArray());

            copy.CopyTo(stack, 2);

            Assert.AreEqual(5, stack.Count);
            Assert.AreEqual(3, copy.Count);

            CollectionAssert.AreEqual(new int[] { 1, 2, 3, 2, 3 }, stack.Select(u => u.Value).ToArray());
            CollectionAssert.AreEqual(new int[] { 1, 2, 3 }, copy.Select(u => u.Value).ToArray());
        }

        [TestMethod]
        public void TestInsertPeek()
        {
            var stack = new RandomAccessStack<Entry>(new ReservedMemory());

            stack.Insert(0, 3);
            stack.Insert(1, 1);
            stack.Insert(1, 2);

            Assert.ThrowsException<InvalidOperationException>(() => stack.Insert(4, 2));

            Assert.AreEqual(3, stack.Count);
            CollectionAssert.AreEqual(new int[] { 1, 2, 3 }, stack.Select(u => u.Value).ToArray());

            Assert.AreEqual(3, stack.Peek(0).Value);
            Assert.AreEqual(2, stack.Peek(1).Value);
            Assert.AreEqual(1, stack.Peek(-1).Value);
        }

        [TestMethod]
        public void TestPopPush()
        {
            var stack = CreateOrderedStack(3);

            Assert.AreEqual(3, stack.Pop().Value);
            Assert.AreEqual(2, stack.Pop().Value);
            Assert.AreEqual(1, stack.Pop().Value);

            Assert.ThrowsException<InvalidOperationException>(() => stack.Pop().Value);
        }

        [TestMethod]
        public void TestTryPopPush()
        {
            var stack = CreateOrderedStack(3);

            Assert.IsTrue(stack.TryPop<Entry>(out var item) && item.Value == 3);
            Assert.IsTrue(stack.TryPop(out item) && item.Value == 2);
            Assert.IsTrue(stack.TryPop(out item) && item.Value == 1);
            Assert.IsFalse(stack.TryPop(out item) && item.Value == 0);
        }

        [TestMethod]
        public void TestRemove()
        {
            var stack = CreateOrderedStack(4);

            Assert.ThrowsException<InvalidOperationException>(() => stack.Remove(4));

            Assert.AreEqual(4, stack.Remove(0).Value);
            Assert.AreEqual(2, stack.Remove(-2).Value);

            CollectionAssert.AreEqual(new int[] { 1, 3 }, stack.Select(u => u.Value).ToArray());

            Assert.ThrowsException<InvalidOperationException>(() => stack.Remove(-3));

            Assert.AreEqual(1, stack.Remove(1).Value);
            Assert.AreEqual(3, stack.Remove(0).Value);
        }

        [TestMethod]
        public void TestSet()
        {
            var stack = CreateOrderedStack(4);

            Assert.ThrowsException<InvalidOperationException>(() => stack.Set(4, int.MaxValue));
            CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4 }, stack.Select(u => u.Value).ToArray());

            stack.Set(0, 11);
            stack.Set(1, 12);
            stack.Set(2, 13);
            stack.Set(3, 14);

            CollectionAssert.AreEqual(new int[] { 14, 13, 12, 11 }, stack.Select(u => u.Value).ToArray());
            Assert.ThrowsException<InvalidOperationException>(() => stack.Set(-5, int.MaxValue));

            stack.Set(-1, 1);
            stack.Set(-2, 2);
            stack.Set(-3, 3);
            stack.Set(-4, 4);

            CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4 }, stack.Select(u => u.Value).ToArray());
        }
    }
}
