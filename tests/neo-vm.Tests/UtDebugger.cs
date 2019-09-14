using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;

namespace Neo.Test
{
    [TestClass]
    public class UtDebugger
    {
        [TestMethod]
        public void TestBreakPoint()
        {
            using (var engine = new ExecutionEngine())
            using (var script = new ScriptBuilder())
            {
                script.Emit(OpCode.NOP);
                script.Emit(OpCode.NOP);
                script.Emit(OpCode.NOP);
                script.Emit(OpCode.NOP);

                engine.LoadScript(script.ToArray());

                var debugger = new Debugger(engine);

                Assert.IsNull(engine.CurrentContext.CallingScript);
                Assert.AreEqual(OpCode.NOP, engine.CurrentContext.NextInstruction.OpCode);

                debugger.AddBreakPoint(engine.CurrentContext.Script, 3);
                debugger.AddBreakPoint(engine.CurrentContext.Script, 4);
                debugger.Execute();

                Assert.IsNull(engine.CurrentContext.CallingScript);
                Assert.AreEqual(OpCode.RET, engine.CurrentContext.NextInstruction.OpCode);
                Assert.AreEqual(3, engine.CurrentContext.InstructionPointer);
                Assert.AreEqual(VMState.BREAK, engine.State);

                debugger.RemoveBreakPoint(engine.CurrentContext.Script, 4);
                debugger.Execute();

                Assert.AreEqual(VMState.HALT, engine.State);
            }
        }

        [TestMethod]
        public void TestWithoutDebugger()
        {
            using (var engine = new ExecutionEngine())
            using (var script = new ScriptBuilder())
            {
                script.Emit(OpCode.NOP);
                script.Emit(OpCode.NOP);
                script.Emit(OpCode.NOP);
                script.Emit(OpCode.NOP);

                engine.LoadScript(script.ToArray());

                Assert.IsNull(engine.CurrentContext.CallingScript);
                Assert.AreEqual(OpCode.NOP, engine.CurrentContext.NextInstruction.OpCode);

                engine.Execute();

                Assert.IsNull(engine.CurrentContext);
                Assert.AreEqual(VMState.HALT, engine.State);
            }
        }

        [TestMethod]
        public void TestStepOver()
        {
            using (var engine = new ExecutionEngine())
            using (var script = new ScriptBuilder())
            {
                /* ┌     CALL 
                   │  ┌> NOT
                   │  │  RET
                   └> │  PUSH0  
                    └─┘  RET */
                script.EmitJump(OpCode.CALL, 5);
                script.Emit(OpCode.NOT);
                script.Emit(OpCode.RET);
                script.Emit(OpCode.PUSH0);
                script.Emit(OpCode.RET);

                engine.LoadScript(script.ToArray());

                var debugger = new Debugger(engine);

                Assert.IsNull(engine.CurrentContext.CallingScript);
                Assert.AreEqual(OpCode.NOT, engine.CurrentContext.NextInstruction.OpCode);

                Assert.AreEqual(VMState.BREAK, debugger.StepOver());

                Assert.IsNull(engine.CurrentContext.CallingScript);
                Assert.AreEqual(3, engine.CurrentContext.InstructionPointer);
                Assert.AreEqual(VMState.BREAK, engine.State);
                Assert.AreEqual(OpCode.RET, engine.CurrentContext.NextInstruction.OpCode);

                debugger.Execute();

                Assert.AreEqual(true, engine.ResultStack.Pop().GetBoolean());
                Assert.AreEqual(VMState.HALT, engine.State);

                // Test step over again

                Assert.AreEqual(VMState.HALT, debugger.StepOver());
                Assert.AreEqual(VMState.HALT, engine.State);
            }
        }

        [TestMethod]
        public void TestStepInto()
        {
            using (var engine = new ExecutionEngine())
            using (var script = new ScriptBuilder())
            {
                /* ┌     CALL
                   │  ┌> NOT 
                   │  │  RET
                   └> │  PUSH0
                    └─┘  RET */
                script.EmitJump(OpCode.CALL, 5);
                script.Emit(OpCode.NOT);
                script.Emit(OpCode.RET);
                script.Emit(OpCode.PUSH0);
                script.Emit(OpCode.RET);

                engine.LoadScript(script.ToArray());

                var debugger = new Debugger(engine);

                var context = engine.CurrentContext;

                Assert.AreEqual(context, engine.CurrentContext);
                Assert.AreEqual(context, engine.EntryContext);
                Assert.IsNull(engine.CurrentContext.CallingScript);
                Assert.AreEqual(OpCode.NOT, engine.CurrentContext.NextInstruction.OpCode);

                Assert.AreEqual(VMState.BREAK, debugger.StepInto());

                Assert.AreNotEqual(context, engine.CurrentContext);
                Assert.AreEqual(context, engine.EntryContext);
                Assert.AreEqual(engine.EntryContext.Script, engine.CurrentContext.CallingScript);
                Assert.AreEqual(OpCode.RET, engine.CurrentContext.NextInstruction.OpCode);

                Assert.AreEqual(VMState.BREAK, debugger.StepInto());
                Assert.AreEqual(VMState.BREAK, debugger.StepInto());

                Assert.AreEqual(context, engine.CurrentContext);
                Assert.AreEqual(context, engine.EntryContext);
                Assert.IsNull(engine.CurrentContext.CallingScript);
                Assert.AreEqual(OpCode.RET, engine.CurrentContext.NextInstruction.OpCode);

                Assert.AreEqual(VMState.BREAK, debugger.StepInto());
                Assert.AreEqual(VMState.HALT, debugger.StepInto());

                Assert.AreEqual(true, engine.ResultStack.Pop().GetBoolean());
                Assert.AreEqual(VMState.HALT, engine.State);

                // Test step into again

                Assert.AreEqual(VMState.HALT, debugger.StepInto());
                Assert.AreEqual(VMState.HALT, engine.State);
            }
        }

        [TestMethod]
        public void TestBreakPointStepOver()
        {
            using (var engine = new ExecutionEngine())
            using (var script = new ScriptBuilder())
            {
                /* ┌     CALL 
                   │  ┌> NOT
                   │  │  RET
                   └>X│  PUSH0
                     └┘  RET */
                script.EmitJump(OpCode.CALL, 5);
                script.Emit(OpCode.NOT);
                script.Emit(OpCode.RET);
                script.Emit(OpCode.PUSH0);
                script.Emit(OpCode.RET);

                engine.LoadScript(script.ToArray());

                var debugger = new Debugger(engine);

                Assert.IsNull(engine.CurrentContext.CallingScript);
                Assert.AreEqual(OpCode.NOT, engine.CurrentContext.NextInstruction.OpCode);

                debugger.AddBreakPoint(engine.CurrentContext.Script, 5);
                Assert.AreEqual(VMState.BREAK, debugger.StepOver());

                Assert.AreEqual(engine.EntryContext.Script, engine.CurrentContext.CallingScript);
                Assert.AreEqual(OpCode.RET, engine.CurrentContext.NextInstruction.OpCode);
                Assert.AreEqual(5, engine.CurrentContext.InstructionPointer);
                Assert.AreEqual(VMState.BREAK, engine.State);

                debugger.Execute();

                Assert.AreEqual(true, engine.ResultStack.Pop().GetBoolean());
                Assert.AreEqual(VMState.HALT, engine.State);
            }
        }
    }
}
