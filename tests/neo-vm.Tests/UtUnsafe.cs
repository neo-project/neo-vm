using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using System;

namespace Neo.Test
{
    [TestClass]
    public class UtUnsafe
    {
        [TestMethod]
        public void NotZero_Null()
        {
            Assert.ThrowsException<ArgumentNullException>(() => Unsafe.NotZero(null));
        }

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
        public void MemoryCopy()
        {
            var from = new byte[0];
            var to = new byte[0];

            Unsafe.MemoryCopy(from, 0, to, 0, 0);
            CollectionAssert.AreEqual(from, to);

            from = new byte[5] { 0x06, 0x07, 0x08, 0x08, 0x09 };
            to = new byte[10] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00 };

            Unsafe.MemoryCopy(from, 0, to, 5, 5);
            CollectionAssert.AreEqual(to, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x08, 0x09 });
        }

        [TestMethod]
        public void MemoryEquals()
        {
            var a = new byte[0];
            var b = new byte[1];

            Assert.IsTrue(Unsafe.MemoryEquals(null, null));
            Assert.IsFalse(Unsafe.MemoryEquals(a, null));
            Assert.IsFalse(Unsafe.MemoryEquals(null, a));

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
