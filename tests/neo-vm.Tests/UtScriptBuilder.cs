using System;
using System.Linq;
using System.Numerics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        public void TestEmitAppCall()
        {
            var scriptHash = RandomHelper.RandBuffer(20);

            using (var script = new ScriptBuilder())
            {
                Assert.ThrowsException<ArgumentException>(() => script.EmitAppCall(new byte[1], true));
            }

            using (var script = new ScriptBuilder())
            {
                script.EmitAppCall(scriptHash, true);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.TAILCALL }.Concat(scriptHash).ToArray(), script.ToArray());
            }

            using (var script = new ScriptBuilder())
            {
                script.EmitAppCall(scriptHash, false);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.APPCALL }.Concat(scriptHash).ToArray(), script.ToArray());
            }
        }

        [TestMethod]
        public void TestEmitSysCall()
        {
            using (var script = new ScriptBuilder())
            {
                Assert.ThrowsException<ArgumentNullException>(() => script.EmitSysCall(null));
            }

            using (var script = new ScriptBuilder())
            {
                script.EmitSysCall("Neo.Runtime.GetTrigger", true);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.SYSCALL, 0x04, 0x75, 0xC8, 0x93, 0xE3 }.ToArray(), script.ToArray());
            }

            for (byte x = 0; x < byte.MaxValue; x++)
            {
                var api = RandomHelper.RandString(x);

                using (var script = new ScriptBuilder())
                {
                    if (x >= 1 && x <= 252)
                    {
                        script.EmitSysCall(api, false);
                        CollectionAssert.AreEqual(new byte[] { (byte)OpCode.SYSCALL, (byte)api.Length }.Concat(Encoding.UTF8.GetBytes(api)).ToArray(), script.ToArray());
                    }
                    else
                    {
                        Assert.ThrowsException<ArgumentException>(() => script.EmitSysCall(RandomHelper.RandString(x), false));
                    }
                }
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
                CollectionAssert.AreEqual(new byte[] { 0x4F }, script.ToArray());
            }

            using (var script = new ScriptBuilder())
            {
                script.EmitPush(BigInteger.Zero);
                CollectionAssert.AreEqual(new byte[] { 0x00 }, script.ToArray());
            }

            for (byte x = 1; x <= 16; x++)
            {
                using (var script = new ScriptBuilder())
                {
                    script.EmitPush(new BigInteger(x));
                    CollectionAssert.AreEqual(new byte[] { (byte)(OpCode.PUSH1 - 1 + x) }, script.ToArray());
                }
            }

            foreach (BigInteger test in new BigInteger[]
                {
                byte.MaxValue,
                short.MinValue, short.MaxValue,
                int.MinValue, int.MaxValue,
                long.MinValue, long.MaxValue,
                sbyte.MinValue, sbyte.MaxValue,
                ushort.MaxValue,
                uint.MaxValue,
                new BigInteger(ulong.MaxValue)
                }
            )
            {
                using (var script = new ScriptBuilder())
                {
                    script.EmitPush(test);
                    CollectionAssert.AreEqual(new byte[] { (byte)test.ToByteArray().Length }.Concat(test.ToByteArray()).ToArray(), script.ToArray());
                }
            }
        }

        [TestMethod]
        public void TestEmitPushBool()
        {
            using (var script = new ScriptBuilder())
            {
                script.EmitPush(true);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.PUSHT }, script.ToArray());
            }

            using (var script = new ScriptBuilder())
            {
                script.EmitPush(false);
                CollectionAssert.AreEqual(new byte[] { (byte)OpCode.PUSHF }, script.ToArray());
            }
        }

        [TestMethod]
        public void TestEmitPushByteArray()
        {
            using (var script = new ScriptBuilder())
            {
                Assert.ThrowsException<ArgumentNullException>(() => script.EmitPush((byte[])null));
            }

            for (byte x = 0; x < 0x4B; x++)
            {
                using (var script = new ScriptBuilder())
                {
                    var data = RandomHelper.RandBuffer(x);

                    script.EmitPush(data);
                    CollectionAssert.AreEqual(new byte[] { x }.Concat(data).ToArray(), script.ToArray());
                }
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

            for (byte x = 0; x < 0x4B; x++)
            {
                using (var script = new ScriptBuilder())
                {
                    var data = RandomHelper.RandString(x);

                    script.EmitPush(data);
                    CollectionAssert.AreEqual(new byte[] { x }.Concat(Encoding.UTF8.GetBytes(data)).ToArray(), script.ToArray());
                }
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