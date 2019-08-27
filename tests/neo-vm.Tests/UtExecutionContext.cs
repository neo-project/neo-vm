using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;

namespace Neo.Test
{
    [TestClass]
    public class UtExecutionContext
    {
        [TestMethod]
        public void StateTest()
        {
            var context = new ExecutionContext(null, null, 0);

            Assert.IsFalse(context.TryGetState<int>(out var i));
            context.SetState(5);
            Assert.AreEqual(5, context.GetState<int>());
            Assert.IsTrue(context.TryGetState(out i));
            Assert.AreEqual(5, i);
        }
    }
}
