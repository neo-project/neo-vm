using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using System.Collections.Generic;

namespace Neo.Test
{
    [TestClass]
    public class UtExecutionContext
    {
        [TestMethod]
        public void StateTest()
        {
            var context = new ExecutionContext(null, null, 0, new ReferenceCounter());

            var stack = context.GetState<Stack<int>>();
            Assert.AreEqual(0, stack.Count);
            stack.Push(100);
            stack = context.GetState<Stack<int>>();
            Assert.AreEqual(100, stack.Pop());
        }
    }
}
