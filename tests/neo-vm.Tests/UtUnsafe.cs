using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;

namespace Neo.Test
{
    [TestClass]
    public class UtUnsafe
    {
        [TestMethod]
        public void NotZero()
        {
            Assert.IsFalse(Unsafe.NotZero(new byte[0]));
            Assert.IsFalse(Unsafe.NotZero(new byte[4]));
            Assert.IsFalse(Unsafe.NotZero(new byte[8]));
            Assert.IsFalse(Unsafe.NotZero(new byte[11]));

            Assert.IsTrue(Unsafe.NotZero(new byte[4] { 0x00, 0x00, 0x00, 0x01 }));
            Assert.IsTrue(Unsafe.NotZero(new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }));
            Assert.IsTrue(Unsafe.NotZero(new byte[11] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }));
        }

        [TestMethod]
        public void MemoryEquals()
        {
            var a = new byte[0];
            var b = new byte[1];

            Assert.IsTrue(Unsafe.MemoryEquals(a, a));
            Assert.IsTrue(Unsafe.MemoryEquals(new byte[0], new byte[0]));
            Assert.IsFalse(Unsafe.MemoryEquals(a, b));

            a = new byte[4];
            b = new byte[4];
            Assert.IsTrue(Unsafe.MemoryEquals(a, b));

            a = new byte[8];
            b = new byte[8];
            Assert.IsTrue(Unsafe.MemoryEquals(a, b));

            a = new byte[11];
            b = new byte[11];
            Assert.IsTrue(Unsafe.MemoryEquals(a, b));

            a = new byte[4] { 0x00, 0x00, 0x00, 0x01 };
            b = new byte[4] { 0x00, 0x00, 0x00, 0x01 };
            Assert.IsTrue(Unsafe.MemoryEquals(a, b));

            a = new byte[4] { 0x00, 0x00, 0x00, 0x01 };
            b = new byte[4] { 0x00, 0x00, 0x00, 0x02 };
            Assert.IsFalse(Unsafe.MemoryEquals(a, b));

            a = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };
            b = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };
            Assert.IsTrue(Unsafe.MemoryEquals(a, b));

            a = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };
            b = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02 };
            Assert.IsFalse(Unsafe.MemoryEquals(a, b));

            a = new byte[11] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };
            b = new byte[11] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };
            Assert.IsTrue(Unsafe.MemoryEquals(a, b));

            a = new byte[11] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };
            b = new byte[11] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02 };
            Assert.IsFalse(Unsafe.MemoryEquals(a, b));
        }
    }
}
