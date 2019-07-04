using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using Neo.VM.Types;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Neo.Test
{
    [TestClass]
    public class UtStackItem
    {
        [TestMethod]
        public void HashCodeTest()
        {
            StackItem itemA = "NEO";
            StackItem itemB = "NEO";
            StackItem itemC = "SmartEconomy";

            Assert.AreEqual(itemA.GetHashCode(), itemB.GetHashCode());
            Assert.AreNotEqual(itemA.GetHashCode(), itemC.GetHashCode());
        }

        [TestMethod]
        public void NullTest()
        {
            StackItem nullItem = new byte[0];
            Assert.AreEqual(nullItem, StackItem.Null);
        }

        [TestMethod]
        public void EqualTest()
        {
            StackItem itemA = "NEO";
            StackItem itemB = "NEO";
            StackItem itemC = "SmartEconomy";

            Assert.AreEqual(itemA, itemB);
            Assert.AreNotEqual(itemA, itemC);
            Assert.AreNotEqual(itemA, new object());
        }

        [TestMethod]
        public void CastTest()
        {
            // Signed integer

            StackItem item = 1;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(1), item.GetBigInteger());

            // Unsigned integer

            item = 2U;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(2), item.GetBigInteger());

            // Signed long

            item = 3L;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(3), item.GetBigInteger());

            // Unsigned long

            item = 4UL;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(4), item.GetBigInteger());

            // BigInteger

            item = BigInteger.MinusOne;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(-1), item.GetBigInteger());

            // Boolean

            item = true;

            Assert.IsInstanceOfType(item, typeof(Boolean));
            Assert.IsTrue(item.GetBoolean());

            // ByteArray

            item = new byte[] { 0x01, 0x02, 0x03 };

            Assert.IsInstanceOfType(item, typeof(ByteArray));
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03 }, item.GetByteArray());

            // String

            item = "NEO";

            Assert.IsInstanceOfType(item, typeof(ByteArray));
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("NEO"), item.GetByteArray());
            Assert.AreEqual("NEO", item.GetString());

            // Array

            item = new StackItem[] { true, false };

            Assert.IsInstanceOfType(item, typeof(Array));
            Assert.IsTrue(((Array)item)[0].GetBoolean());
            Assert.IsFalse(((Array)item)[1].GetBoolean());

            // List

            item = new List<StackItem>(new StackItem[] { true, false });

            Assert.IsInstanceOfType(item, typeof(Array));
            Assert.IsTrue(((Array)item)[0].GetBoolean());
            Assert.IsFalse(((Array)item)[1].GetBoolean());
        }
    }
}