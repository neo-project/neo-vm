using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using System;
using System.Linq;

namespace Neo.Test
{
    [TestClass]
    public class UtOpCode
    {
        [TestMethod]
        public void EnsureReserved()
        {
            OpCode[] reserved = { (OpCode)0xAC, (OpCode)0xAE };
            Assert.IsTrue(reserved.All(p => !Enum.IsDefined(typeof(OpCode), p)));
        }
    }
}
