using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using System;
using System.Text;

namespace Neo.Test
{
    [TestClass]
    public class UtScript
    {
        [TestMethod]
        public void Conversion()
        {
            byte[] rawScript;
            using (var builder = new ScriptBuilder())
            {
                builder.Emit(OpCode.PUSH0);
                builder.Emit(OpCode.CALL, new byte[] { 0x00, 0x01 });
                builder.EmitSysCall(123);

                rawScript = builder.ToArray();
            }

            var script = new Script(rawScript);

            byte[] scriptConversion = script;
            CollectionAssert.AreEqual(scriptConversion, rawScript);
        }

        [TestMethod]
        public void Parse()
        {
            Script script;

            using (var builder = new ScriptBuilder())
            {
                builder.Emit(OpCode.PUSH0);
                builder.Emit(OpCode.CALL, new byte[] { 0x00, 0x01 });
                builder.EmitSysCall(123);

                script = new Script(builder.ToArray());
            }

            Assert.AreEqual(9, script.Length);

            var ins = script.GetInstruction(0);

            Assert.AreEqual(ins.OpCode, OpCode.PUSH0);
            Assert.AreEqual(ins.Operand, null);
            Assert.AreEqual(ins.Size, 1);
            Assert.ThrowsException<ArgumentNullException>(() => { var x = ins.TokenI16; });
            Assert.ThrowsException<ArgumentNullException>(() => { var x = ins.TokenString; });
            Assert.ThrowsException<ArgumentNullException>(() => { var x = ins.TokenU32; });

            ins = script.GetInstruction(1);

            Assert.AreEqual(ins.OpCode, OpCode.CALL);
            CollectionAssert.AreEqual(ins.Operand, new byte[] { 0x00, 0x01 });
            Assert.AreEqual(ins.Size, 3);
            Assert.AreEqual(ins.TokenI16, 256);
            Assert.AreEqual(ins.TokenString, Encoding.ASCII.GetString(new byte[] { 0x00, 0x01 }));
            Assert.ThrowsException<ArgumentException>(() => { var x = ins.TokenU32; });

            ins = script.GetInstruction(4);

            Assert.AreEqual(ins.OpCode, OpCode.SYSCALL);
            CollectionAssert.AreEqual(ins.Operand, new byte[] { 123, 0x00, 0x00, 0x00 });
            Assert.AreEqual(ins.Size, 5);
            Assert.AreEqual(ins.TokenI16, 123);
            Assert.AreEqual(ins.TokenString, Encoding.ASCII.GetString(new byte[] { 123, 0x00, 0x00, 0x00 }));
            Assert.AreEqual(ins.TokenU32, 123U);

            ins = script.GetInstruction(100);

            Assert.AreSame(ins, Instruction.RET);
        }
    }
}
