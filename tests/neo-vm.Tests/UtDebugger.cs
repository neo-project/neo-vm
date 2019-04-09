using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Test.Types;
using Neo.VM;

namespace Neo.Test
{
    [TestClass]
    public class UtDebugger
    {
        [TestMethod]
        public void TestBreakPoint()
        {
            using (var engine = new ExecutionEngine(null, Crypto.Default, null, null))
            using (var script = new ScriptBuilder())
            {
                script.Emit(OpCode.NOP);
                script.Emit(OpCode.NOP);
                script.Emit(OpCode.NOP);
                script.Emit(OpCode.NOP);

                engine.LoadScript(script.ToArray());

                var debugger = new Debugger(engine);

                debugger.AddBreakPoint(engine.CurrentContext.ScriptHash, 3);
                debugger.AddBreakPoint(engine.CurrentContext.ScriptHash, 4);
                debugger.Execute();

                Assert.AreEqual(3, engine.CurrentContext.InstructionPointer);
                Assert.AreEqual(VMState.BREAK, engine.State);

                debugger.RemoveBreakPoint(engine.CurrentContext.ScriptHash, 4);
                debugger.Execute();

                Assert.AreEqual(VMState.HALT, engine.State);
            }
        }
    }
}