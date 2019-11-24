using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using System;
using System.Diagnostics;
using System.Text;

namespace Neo.Test
{
    [TestClass]
    public class UtScript
    {
        [TestMethod]
        public void MaxItemTestOk()
        {
            TestWithArray(false, 1);
            TestWithPUSHPOP(false, 1);
        }

        [TestMethod]
        public void MaxItemTestFail()
        {
            TestWithArray(true, 1);
            TestWithPUSHPOP(true, 1);
        }

        [TestMethod]
        public void BenchArray()
        {
            TestWithArray(false, 1000);
        }

        [TestMethod]
        public void BenchPushPop()
        {
            TestWithPUSHPOP(false, 1000);
        }

        public void TestWithArray(bool error, int iterations = 1)
        {
            var items = 1000;
            var script = new ScriptBuilder();

            for (int x = 0; x < iterations; x++)
            {
                script.Emit(OpCode.PUSH0);
                script.Emit(OpCode.NEWARRAY);
                script.Emit(OpCode.TOALTSTACK);

                for (int y = 0; y < (error ? items : items - 1); y++)
                {
                    script.Emit(OpCode.DUPFROMALTSTACK);
                    script.Emit(OpCode.PUSH0);
                    script.Emit(OpCode.APPEND); // Force stack count
                }

                script.Emit(OpCode.FROMALTSTACK);
                script.Emit(OpCode.DROP);
            }

            Stopwatch sw = Stopwatch.StartNew();

            var engine = new ExecutionEngine();
            engine.StackItemMemory.Reserved = items;

            engine.LoadScript(script.ToArray());
            Assert.AreEqual(error ? VMState.FAULT : VMState.HALT, engine.Execute());
            sw.Stop();

            Console.WriteLine(sw.Elapsed);
        }

        public void TestWithPUSHPOP(bool error, int iterations = 1)
        {
            var items = 2048;
            var script = new ScriptBuilder();

            for (int y = 0; y < (error ? items + 1 : items - 1); y++)
            {
                script.Emit(OpCode.PUSH0);
            }

            for (int y = 0; y < (error ? items : items - 1); y++)
            {
                script.Emit(OpCode.DROP);
            }

            Stopwatch sw = Stopwatch.StartNew();

            var engine = new ExecutionEngine();
            engine.StackItemMemory.Reserved = items;

            engine.LoadScript(script.ToArray());
            Assert.AreEqual(error ? VMState.FAULT : VMState.HALT, engine.Execute());
            sw.Stop();

            Console.WriteLine(sw.Elapsed);
        }

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
            Assert.IsTrue(ins.Operand.IsEmpty);
            Assert.AreEqual(ins.Size, 1);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { var x = ins.TokenI16; });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { var x = ins.TokenU32; });

            ins = script.GetInstruction(1);

            Assert.AreEqual(ins.OpCode, OpCode.CALL);
            CollectionAssert.AreEqual(ins.Operand.ToArray(), new byte[] { 0x00, 0x01 });
            Assert.AreEqual(ins.Size, 3);
            Assert.AreEqual(ins.TokenI16, 256);
            Assert.AreEqual(ins.TokenString, Encoding.ASCII.GetString(new byte[] { 0x00, 0x01 }));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { var x = ins.TokenU32; });

            ins = script.GetInstruction(4);

            Assert.AreEqual(ins.OpCode, OpCode.SYSCALL);
            CollectionAssert.AreEqual(ins.Operand.ToArray(), new byte[] { 123, 0x00, 0x00, 0x00 });
            Assert.AreEqual(ins.Size, 5);
            Assert.AreEqual(ins.TokenI16, 123);
            Assert.AreEqual(ins.TokenString, Encoding.ASCII.GetString(new byte[] { 123, 0x00, 0x00, 0x00 }));
            Assert.AreEqual(ins.TokenU32, 123U);

            ins = script.GetInstruction(100);

            Assert.AreSame(ins, Instruction.RET);
        }
    }
}
