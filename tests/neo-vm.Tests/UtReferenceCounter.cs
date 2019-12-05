using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;

namespace Neo.Test
{
    [TestClass]
    public class UtReferenceCounter
    {
        [TestMethod]
        public void TestCircularReferences()
        {
            using ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(0); //{0}{}:1
            sb.Emit(OpCode.NEWARRAY); //{A[]}|{}:1
            sb.Emit(OpCode.DUP); //{A[],A[]}|{}:2
            sb.Emit(OpCode.DUP); //{A[],A[],A[]}|{}:3
            sb.Emit(OpCode.APPEND); //{A[A]}|{}:2
            sb.Emit(OpCode.DUP); //{A[A],A[A]}|{}:3
            sb.EmitPush(0); //{A[A],A[A],0}|{}:4
            sb.Emit(OpCode.NEWARRAY); //{A[A],A[A],B[]}|{}:4
            sb.Emit(OpCode.TOALTSTACK); //{A[A],A[A]}|{B[]}:4
            sb.Emit(OpCode.DUPFROMALTSTACK); //{A[A],A[A],B[]}|{B[]}:5
            sb.Emit(OpCode.APPEND); //{A[A,B]}|{B[]}:4
            sb.Emit(OpCode.DUPFROMALTSTACK); //{A[A,B],B[]}|{B[]}:5
            sb.EmitPush(0); //{A[A,B],B[],0}|{B[]}:6
            sb.Emit(OpCode.NEWARRAY); //{A[A,B],B[],C[]}|{B[]}:6
            sb.Emit(OpCode.TUCK); //{A[A,B],C[],B[],C[]}|{B[]}:7
            sb.Emit(OpCode.APPEND); //{A[A,B],C[]}|{B[C]}:6
            sb.EmitPush(0); //{A[A,B],C[],0}|{B[C]}:7
            sb.Emit(OpCode.NEWARRAY); //{A[A,B],C[],D[]}|{B[C]}:7
            sb.Emit(OpCode.TUCK); //{A[A,B],D[],C[],D[]}|{B[C]}:8
            sb.Emit(OpCode.APPEND); //{A[A,B],D[]}|{B[C[D]]}:7
            sb.Emit(OpCode.DUPFROMALTSTACK); //{A[A,B],D[],B[C]}|{B[C[D]]}:8
            sb.Emit(OpCode.APPEND); //{A[A,B]}|{B[C[D[B]]]}:7
            sb.Emit(OpCode.FROMALTSTACK); //{A[A,B],B[C[D[B]]]}|{}:7
            sb.Emit(OpCode.DROP); //{A[A,B[C[D[B]]]]}|{}:6
            sb.Emit(OpCode.DUP); //{A[A,B[C[D[B]]]],A[A,B]}|{}:7
            sb.EmitPush(1); //{A[A,B[C[D[B]]]],A[A,B],1}|{}:8
            sb.Emit(OpCode.REMOVE); //{A[A]}|{}:2
            sb.Emit(OpCode.TOALTSTACK); //{}|{A[A]}:2
            sb.Emit(OpCode.RET); //{}:0
            using ExecutionEngine engine = new ExecutionEngine();
            Debugger debugger = new Debugger(engine);
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(1, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(1, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(2, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(3, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(2, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(3, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(4, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(4, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(4, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(5, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(4, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(5, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(6, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(6, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(7, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(6, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(7, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(7, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(8, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(7, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(8, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(7, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(7, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(6, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(7, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(8, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(2, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(2, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.HALT, debugger.Execute());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);
        }

        [TestMethod]
        public void TestRemoveReferrer()
        {
            using ScriptBuilder sb = new ScriptBuilder();
            sb.EmitPush(0); //{0}{}:1
            sb.Emit(OpCode.NEWARRAY); //{A[]}|{}:1
            sb.Emit(OpCode.DUP); //{A[],A[]}|{}:2
            sb.EmitPush(0); //{A[],A[],0}|{}:3
            sb.Emit(OpCode.NEWARRAY); //{A[],A[],B[]}|{}:3
            sb.Emit(OpCode.TOALTSTACK); //{A[],A[]}|{B[]}:3
            sb.Emit(OpCode.DUPFROMALTSTACK); //{A[],A[],B[]}|{B[]}:4
            sb.Emit(OpCode.APPEND); //{A[B]}|{B[]}:3
            sb.Emit(OpCode.DROP); //{}|{B[]}:1
            sb.Emit(OpCode.RET); //{}:0
            using ExecutionEngine engine = new ExecutionEngine();
            Debugger debugger = new Debugger(engine);
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(1, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(1, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(2, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(3, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(3, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(3, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(4, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(3, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.BREAK, debugger.StepInto());
            Assert.AreEqual(1, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.HALT, debugger.Execute());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);
        }
    }
}
