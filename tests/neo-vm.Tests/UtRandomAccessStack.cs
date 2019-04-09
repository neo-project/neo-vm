using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;

namespace Neo.Test
{
    [TestClass]
    public class UtRandomAccessStack
    {
        RandomAccessStack<int> CreateOrderedStack(int count)
        {
            var check = new int[count];
            var stack = new RandomAccessStack<int>();

            for (int x = 1; x <= count; x++)
            {
                stack.Push(x);
                check[x - 1] = x;
            }

            Assert.AreEqual(count, stack.Count);
            CollectionAssert.AreEqual(check, stack.Select(u => u).ToArray());

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
            var copy = new RandomAccessStack<int>();

            stack.CopyTo(copy, 0);

            Assert.AreEqual(3, stack.Count);
            Assert.AreEqual(0, copy.Count);
            CollectionAssert.AreEqual(new int[] { 1, 2, 3 }, ((IEnumerable<int>)stack).Select(u => u).ToArray());

            stack.CopyTo(copy, -1);

            Assert.AreEqual(3, stack.Count);
            Assert.AreEqual(3, copy.Count);
            CollectionAssert.AreEqual(new int[] { 1, 2, 3 }, ((IEnumerable<int>)stack).Select(u => u).ToArray());

            // Test IEnumerable

            var enumerable = (IEnumerable)copy;
            var enumerator = enumerable.GetEnumerator();

            CollectionAssert.AreEqual(new int[] { 1, 2, 3 }, GetEnumerable(enumerator).Cast<int>().Select(u => u).ToArray());

            copy.CopyTo(stack, 2);

            Assert.AreEqual(5, stack.Count);
            Assert.AreEqual(3, copy.Count);

            CollectionAssert.AreEqual(new int[] { 1, 2, 3, 2, 3 }, stack.Select(u => u).ToArray());
            CollectionAssert.AreEqual(new int[] { 1, 2, 3 }, copy.Select(u => u).ToArray());
        }

        [TestMethod]
        public void TestInsertPeek()
        {
            var stack = new RandomAccessStack<int>();

            stack.Insert(0, 3);
            stack.Insert(1, 1);
            stack.Insert(1, 2);

            Assert.ThrowsException<InvalidOperationException>(() => stack.Insert(4, 2));

            Assert.AreEqual(3, stack.Count);
            CollectionAssert.AreEqual(new int[] { 1, 2, 3 }, stack.Select(u => u).ToArray());

            Assert.AreEqual(3, stack.Peek(0));
            Assert.AreEqual(2, stack.Peek(1));
            Assert.AreEqual(1, stack.Peek(-1));
        }

        [TestMethod]
        public void TestPopPush()
        {
            var stack = CreateOrderedStack(3);

            Assert.AreEqual(3, stack.Pop());
            Assert.AreEqual(2, stack.Pop());
            Assert.AreEqual(1, stack.Pop());

            Assert.ThrowsException<InvalidOperationException>(() => stack.Pop());
        }

        [TestMethod]
        public void TestRemove()
        {
            var stack = CreateOrderedStack(4);

            Assert.ThrowsException<InvalidOperationException>(() => stack.Remove(4));

            Assert.AreEqual(4, stack.Remove(0));
            Assert.AreEqual(2, stack.Remove(-2));

            CollectionAssert.AreEqual(new int[] { 1, 3 }, stack.Select(u => u).ToArray());

            Assert.ThrowsException<InvalidOperationException>(() => stack.Remove(-3));

            Assert.AreEqual(1, stack.Remove(1));
            Assert.AreEqual(3, stack.Remove(0));
        }

        [TestMethod]
        public void TestSet()
        {
            var stack = CreateOrderedStack(4);

            Assert.ThrowsException<InvalidOperationException>(() => stack.Set(4, int.MaxValue));
            CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4 }, stack.Select(u => u).ToArray());

            stack.Set(0, 11);
            stack.Set(1, 12);
            stack.Set(2, 13);
            stack.Set(3, 14);

            CollectionAssert.AreEqual(new int[] { 14, 13, 12, 11 }, stack.Select(u => u).ToArray());
            Assert.ThrowsException<InvalidOperationException>(() => stack.Set(-5, int.MaxValue));

            stack.Set(-1, 1);
            stack.Set(-2, 2);
            stack.Set(-3, 3);
            stack.Set(-4, 4);

            CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4 }, stack.Select(u => u).ToArray());
        }
    }
}