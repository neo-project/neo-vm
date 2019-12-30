using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections;
using System.Linq;

namespace Neo.Test
{
    [TestClass]
    public class UtEvaluationStack
    {
        EvaluationStack CreateOrderedStack(int count)
        {
            var check = new Integer[count];
            var stack = new EvaluationStack(new ReferenceCounter());

            for (int x = 1; x <= count; x++)
            {
                stack.Push(x);
                check[x - 1] = x;
            }

            Assert.AreEqual(count, stack.Count);
            CollectionAssert.AreEqual(check, stack.ToArray());

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
            var copy = new EvaluationStack(new ReferenceCounter());

            stack.CopyTo(copy, 0);

            Assert.AreEqual(3, stack.Count);
            Assert.AreEqual(0, copy.Count);
            CollectionAssert.AreEqual(new Integer[] { 1, 2, 3 }, stack.ToArray());

            stack.CopyTo(copy, -1);

            Assert.AreEqual(3, stack.Count);
            Assert.AreEqual(3, copy.Count);
            CollectionAssert.AreEqual(new Integer[] { 1, 2, 3 }, stack.ToArray());

            // Test IEnumerable

            var enumerable = (IEnumerable)copy;
            var enumerator = enumerable.GetEnumerator();

            CollectionAssert.AreEqual(new Integer[] { 1, 2, 3 }, GetEnumerable(enumerator).Cast<Integer>().ToArray());

            copy.CopyTo(stack, 2);

            Assert.AreEqual(5, stack.Count);
            Assert.AreEqual(3, copy.Count);

            CollectionAssert.AreEqual(new Integer[] { 1, 2, 3, 2, 3 }, stack.ToArray());
            CollectionAssert.AreEqual(new Integer[] { 1, 2, 3 }, copy.ToArray());
        }

        [TestMethod]
        public void TestInsertPeek()
        {
            var stack = new EvaluationStack(new ReferenceCounter());

            stack.Insert(0, 3);
            stack.Insert(1, 1);
            stack.Insert(1, 2);

            Assert.ThrowsException<InvalidOperationException>(() => stack.Insert(4, 2));

            Assert.AreEqual(3, stack.Count);
            CollectionAssert.AreEqual(new Integer[] { 1, 2, 3 }, stack.ToArray());

            Assert.AreEqual(3, stack.Peek(0));
            Assert.AreEqual(2, stack.Peek(1));
            Assert.AreEqual(1, stack.Peek(-1));

            Assert.ThrowsException<InvalidOperationException>(() => stack.Peek(-4));
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
        public void TestTryPopPush()
        {
            var stack = CreateOrderedStack(3);

            Assert.IsTrue(stack.TryPop(out Integer item) && item.Equals(3));
            Assert.IsTrue(stack.TryPop(out item) && item.Equals(2));
            Assert.IsTrue(stack.TryPop(out item) && item.Equals(1));
            Assert.IsFalse(stack.TryPop(out item) && item.Equals(0));
        }

        [TestMethod]
        public void TestTryRemove()
        {
            var stack = CreateOrderedStack(3);

            Assert.IsTrue(stack.TryRemove(0, out Integer item) && item.Equals(3));
            Assert.IsTrue(stack.TryRemove(0, out item) && item.Equals(2));
            Assert.IsTrue(stack.TryRemove(-1, out item) && item.Equals(1));
            Assert.IsFalse(stack.TryRemove(0, out item) && item.Equals(0));
            Assert.IsFalse(stack.TryRemove(-1, out item) && item.Equals(0));
        }

        [TestMethod]
        public void TestReverse()
        {
            var stack = CreateOrderedStack(3);

            Assert.IsTrue(stack.Reverse(3));
            Assert.IsTrue(stack.TryPop(out Integer item) && item.Equals(1));
            Assert.IsTrue(stack.TryPop(out item) && item.Equals(2));
            Assert.IsTrue(stack.TryPop(out item) && item.Equals(3));
            Assert.IsFalse(stack.TryPop(out item) && item.Equals(0));

            stack = CreateOrderedStack(3);

            Assert.IsFalse(stack.Reverse(-1));
            Assert.IsFalse(stack.Reverse(4));

            Assert.IsTrue(stack.Reverse(1));
            Assert.IsTrue(stack.TryPop(out item) && item.Equals(3));
            Assert.IsTrue(stack.TryPop(out item) && item.Equals(2));
            Assert.IsTrue(stack.TryPop(out item) && item.Equals(1));
            Assert.IsFalse(stack.TryPop(out item) && item.Equals(0));
        }
    }
}
