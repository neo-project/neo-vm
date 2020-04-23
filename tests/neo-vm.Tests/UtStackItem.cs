using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Numerics;

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

            Assert.IsTrue(itemA.GetHashCode() == itemB.GetHashCode());
            Assert.IsTrue(itemA.GetHashCode() != itemC.GetHashCode());

            itemA = new VM.Types.Buffer(1);
            itemB = new VM.Types.Buffer(1);
            itemC = new VM.Types.Buffer(123);

            Assert.IsTrue(itemA.GetHashCode() == itemB.GetHashCode());
            Assert.IsTrue(itemA.GetHashCode() != itemC.GetHashCode());

            itemA = true;
            itemB = true;
            itemC = false;

            Assert.IsTrue(itemA.GetHashCode() == itemB.GetHashCode());
            Assert.IsTrue(itemA.GetHashCode() != itemC.GetHashCode());

            itemA = 1;
            itemB = 1;
            itemC = 123;

            Assert.IsTrue(itemA.GetHashCode() == itemB.GetHashCode());
            Assert.IsTrue(itemA.GetHashCode() != itemC.GetHashCode());

            itemA = new Null();

            Assert.ThrowsException<NotSupportedException>(() => itemA.GetHashCode());

            itemA = new VM.Types.Array();

            Assert.ThrowsException<NotSupportedException>(() => itemA.GetHashCode());

            itemA = new Struct();

            Assert.ThrowsException<NotSupportedException>(() => itemA.GetHashCode());

            itemA = new Map();

            Assert.ThrowsException<NotSupportedException>(() => itemA.GetHashCode());

            itemA = new InteropInterface(123);

            Assert.ThrowsException<NotSupportedException>(() => itemA.GetHashCode());

            var script = new Script(new byte[0]);
            itemA = new Pointer(script, 123);
            itemB = new Pointer(script, 123);
            itemC = new Pointer(script, 1234);

            Assert.IsTrue(itemA.GetHashCode() == itemB.GetHashCode());
            Assert.IsTrue(itemA.GetHashCode() != itemC.GetHashCode());
        }

        [TestMethod]
        public void NullTest()
        {
            StackItem nullItem = new byte[0];
            Assert.AreNotEqual(StackItem.Null, nullItem);

            nullItem = new Null();
            Assert.AreEqual(StackItem.Null, nullItem);
        }

        [TestMethod]
        public void EqualTest()
        {
            StackItem itemA = "NEO";
            StackItem itemB = "NEO";
            StackItem itemC = "SmartEconomy";
            StackItem itemD = "Smarteconomy";
            StackItem itemE = "smarteconomy";

            Assert.IsTrue(itemA.Equals(itemB));
            Assert.IsFalse(itemA.Equals(itemC));
            Assert.IsFalse(itemC.Equals(itemD));
            Assert.IsFalse(itemD.Equals(itemE));
            Assert.IsFalse(itemA.Equals(new object()));
        }

        [TestMethod]
        public void CastTest()
        {
            // Signed integer

            StackItem item = int.MaxValue;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(int.MaxValue), ((Integer)item).ToBigInteger());

            // Unsigned integer

            item = uint.MaxValue;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(uint.MaxValue), ((Integer)item).ToBigInteger());

            // Signed long

            item = long.MaxValue;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(long.MaxValue), ((Integer)item).ToBigInteger());

            // Unsigned long

            item = ulong.MaxValue;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(ulong.MaxValue), ((Integer)item).ToBigInteger());

            // BigInteger

            item = BigInteger.MinusOne;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(-1), ((Integer)item).ToBigInteger());

            // Boolean

            item = true;

            Assert.IsInstanceOfType(item, typeof(VM.Types.Boolean));
            Assert.IsTrue(item.ToBoolean());

            // ByteString

            item = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 };

            Assert.IsInstanceOfType(item, typeof(ByteString));
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 }, ((ByteString)item).Span.ToArray());
        }
    }
}
