using System;
using System.Linq;
using System.Numerics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Test.Extensions;
using Neo.Test.Helpers;
using Neo.VM;

namespace Neo.Test
{
    [TestClass]
    public class UtScriptBuilder
    {
        [TestMethod]
        public void TestEmit()
        {
            using (var script = new ScriptBuilder())
            {
                Assert.AreEqual(0, script.Offset);
                script.Emit(OpCode.NOP);
                Assert.AreEqual(1, script.Offset);

                CollectionAssert.AreEqual(new byte[] { 0x61 }, script.ToArray());
            }

            using (var script = new ScriptBuilder())
            {
                script.Emit(OpCode.NOP, new byte[] { 0x66 });
                CollectionAssert.AreEqual(new byte[] { 0x61, 0x66 }, script.ToArray());
            }
        }

        [TestMethod]
        public void TestEmitSysCall()
        {
            using (var script = new ScriptBuilder())
            {
                script.EmitSysCall(0xE393C875);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.SYSCALL, 0x75, 0xC8, 0x93, 0xE3 }.ToArray(), script.ToArray());
            }
        }

        [TestMethod]
        public void TestEmitJump()
        {
            var offset = RandomHelper.RandInt16();

            foreach (OpCode op in Enum.GetValues(typeof(OpCode)))
            {
                using (var script = new ScriptBuilder())
                {
                    if (op != OpCode.JMP && op != OpCode.JMPIF && op != OpCode.JMPIFNOT && op != OpCode.CALL)
                    {
                        Assert.ThrowsException<ArgumentException>(() => script.EmitJump(op, offset));
                    }
                    else
                    {
                        script.EmitJump(op, offset);
                        CollectionAssert.AreEqual(new byte[] { (byte)op }.Concat(BitConverter.GetBytes(offset)).ToArray(), script.ToArray());
                    }
                }
            }
        }

        [TestMethod]
        public void TestEmitPushBigInteger()
        {
            using (var script = new ScriptBuilder())
            {
                script.EmitPush(BigInteger.MinusOne);
                CollectionAssert.AreEqual(new byte[] { 0x0F }, script.ToArray());
            }

            using (var script = new ScriptBuilder())
            {
                script.EmitPush(BigInteger.Zero);
                CollectionAssert.AreEqual(new byte[] { 0x10 }, script.ToArray());
            }

            for (byte x = 1; x <= 16; x++)
            {
                using (var script = new ScriptBuilder())
                {
                    script.EmitPush(new BigInteger(x));
                    CollectionAssert.AreEqual(new byte[] { (byte)(OpCode.PUSH0 + x) }, script.ToArray());
                }
            }

            CollectionAssert.AreEqual("0080".FromHexString(), new ScriptBuilder().EmitPush(sbyte.MinValue).ToArray());
            CollectionAssert.AreEqual("007f".FromHexString(), new ScriptBuilder().EmitPush(sbyte.MaxValue).ToArray());
            CollectionAssert.AreEqual("01ff00".FromHexString(), new ScriptBuilder().EmitPush(byte.MaxValue).ToArray());
            CollectionAssert.AreEqual("010080".FromHexString(), new ScriptBuilder().EmitPush(short.MinValue).ToArray());
            CollectionAssert.AreEqual("01ff7f".FromHexString(), new ScriptBuilder().EmitPush(short.MaxValue).ToArray());
            CollectionAssert.AreEqual("02ffff0000".FromHexString(), new ScriptBuilder().EmitPush(ushort.MaxValue).ToArray());
            CollectionAssert.AreEqual("0200000080".FromHexString(), new ScriptBuilder().EmitPush(int.MinValue).ToArray());
            CollectionAssert.AreEqual("02ffffff7f".FromHexString(), new ScriptBuilder().EmitPush(int.MaxValue).ToArray());
            CollectionAssert.AreEqual("03ffffffff00000000".FromHexString(), new ScriptBuilder().EmitPush(uint.MaxValue).ToArray());
            CollectionAssert.AreEqual("030000000000000080".FromHexString(), new ScriptBuilder().EmitPush(long.MinValue).ToArray());
            CollectionAssert.AreEqual("03ffffffffffffff7f".FromHexString(), new ScriptBuilder().EmitPush(long.MaxValue).ToArray());
            CollectionAssert.AreEqual("04ffffffffffffffff0000000000000000".FromHexString(), new ScriptBuilder().EmitPush(ulong.MaxValue).ToArray());
        }

        [TestMethod]
        public void TestEmitPushBool()
        {
            using (var script = new ScriptBuilder())
            {
                script.EmitPush(true);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.PUSH1 }, script.ToArray());
            }

            using (var script = new ScriptBuilder())
            {
                script.EmitPush(false);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.PUSH0 }, script.ToArray());
            }
        }

        [TestMethod]
        public void TestEmitPushByteArray()
        {
            using (var script = new ScriptBuilder())
            {
                Assert.ThrowsException<ArgumentNullException>(() => script.EmitPush((byte[])null));
            }

            using (var script = new ScriptBuilder())
            {
                var data = RandomHelper.RandBuffer(0x4C);

                script.EmitPush(data);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.PUSHDATA1, (byte)data.Length }.Concat(data).ToArray(), script.ToArray());
            }

            using (var script = new ScriptBuilder())
            {
                var data = RandomHelper.RandBuffer(0x100);

                script.EmitPush(data);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.PUSHDATA2 }.Concat(BitConverter.GetBytes((short)data.Length)).Concat(data).ToArray(), script.ToArray());
            }

            using (var script = new ScriptBuilder())
            {
                var data = RandomHelper.RandBuffer(0x10000);

                script.EmitPush(data);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.PUSHDATA4 }.Concat(BitConverter.GetBytes(data.Length)).Concat(data).ToArray(), script.ToArray());
            }
        }

        [TestMethod]
        public void TestEmitPushString()
        {
            using (var script = new ScriptBuilder())
            {
                Assert.ThrowsException<ArgumentNullException>(() => script.EmitPush((string)null));
            }

            using (var script = new ScriptBuilder())
            {
                var data = RandomHelper.RandString(0x4C);

                script.EmitPush(data);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.PUSHDATA1, (byte)data.Length }.Concat(Encoding.UTF8.GetBytes(data)).ToArray(), script.ToArray());
            }

            using (var script = new ScriptBuilder())
            {
                var data = RandomHelper.RandString(0x100);

                script.EmitPush(data);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.PUSHDATA2 }.Concat(BitConverter.GetBytes((short)data.Length)).Concat(Encoding.UTF8.GetBytes(data)).ToArray(), script.ToArray());
            }

            using (var script = new ScriptBuilder())
            {
                var data = RandomHelper.RandString(0x10000);

                script.EmitPush(data);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.PUSHDATA4 }.Concat(BitConverter.GetBytes(data.Length)).Concat(Encoding.UTF8.GetBytes(data)).ToArray(), script.ToArray());
            }
        }
    }
}
