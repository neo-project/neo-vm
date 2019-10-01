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
            Assert.AreNotEqual(nullItem, StackItem.Null);

            nullItem = new Null();
            Assert.AreEqual(nullItem, StackItem.Null);
        }

        [TestMethod]
        public void EqualTest()
        {
            StackItem itemA = "NEO";
            StackItem itemB = "NEO";
            StackItem itemC = "SmartEconomy";
            StackItem itemD = "Smarteconomy";
            StackItem itemE = "smarteconomy";

            Assert.AreEqual(itemA, itemB);
            Assert.AreNotEqual(itemA, itemC);
            Assert.AreNotEqual(itemC, itemD);
            Assert.AreNotEqual(itemD, itemE);
            Assert.AreNotEqual(itemA, new object());
        }

        [TestMethod]
        public void CastTest()
        {
            // Signed integer

            StackItem item = int.MaxValue;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(int.MaxValue), item.GetBigInteger());

            // Unsigned integer

            item = uint.MaxValue;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(uint.MaxValue), item.GetBigInteger());

            // Signed long

            item = long.MaxValue;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(long.MaxValue), item.GetBigInteger());

            // Unsigned long

            item = ulong.MaxValue;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(ulong.MaxValue), item.GetBigInteger());

            // BigInteger

            item = BigInteger.MinusOne;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(-1), item.GetBigInteger());

            // Boolean

            item = true;

            Assert.IsInstanceOfType(item, typeof(Boolean));
            Assert.IsTrue(item.GetBoolean());

            // ByteArray

            item = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 };

            Assert.IsInstanceOfType(item, typeof(ByteArray));
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 }, item.GetByteArray());

            // String

            item = "NEO - 种智能经济分布式网络";

            Assert.IsInstanceOfType(item, typeof(ByteArray));
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("NEO - 种智能经济分布式网络"), item.GetByteArray());
            Assert.AreEqual("NEO - 种智能经济分布式网络", item.GetString());

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

            // Interop

            var interop = new InteropInterface<Dictionary<int, int>>(new Dictionary<int, int>() { { 1, 1 } });

            Dictionary<int, int> value = interop;
            Assert.AreEqual(1, value.Count);

        }
    }
}
