// Copyright (C) 2015-2026 The Neo Project.
//
// UT_DebuggerMiddleware.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Test.Types;
using Neo.VM;
using Neo.VM.Logging;
using Neo.VM.Middleware;
using System;
using System.Collections.Generic;

namespace Neo.Test;

[TestClass]
public class UT_DebuggerMiddleware
{
    [TestMethod]
    public void TestEngineExecuteStopsAtBreakpoint()
    {
        using ExecutionEngine engine = new();
        using ScriptBuilder script = new();
        script.Emit(OpCode.NOP);
        script.Emit(OpCode.NOP);
        script.Emit(OpCode.NOP);
        script.Emit(OpCode.RET);

        engine.LoadScript(script.ToArray());

        DebuggerMiddleware debugger = new(engine);
        debugger.AddBreakPoint(engine.CurrentContext!.Script, 2);

        Assert.AreEqual(VMState.BREAK, engine.Execute());
        Assert.AreEqual(2, engine.CurrentContext!.InstructionPointer);

        debugger.Continue();
        Assert.AreEqual(VMState.HALT, engine.Execute());
    }

    [TestMethod]
    public void TestOnBreakpointEvent()
    {
        using ExecutionEngine engine = new();
        using ScriptBuilder script = new();
        script.EmitPush(1);
        script.EmitPush(2);
        script.Emit(OpCode.ADD);
        script.Emit(OpCode.RET);

        engine.LoadScript(script.ToArray());

        var hits = new List<int>();
        DebuggerMiddleware debugger = new(engine);
        debugger.AddBreakpoint(2);
        debugger.OnBreakpoint += (_, e) => hits.Add(e.Context.InstructionPointer);

        Assert.AreEqual(VMState.BREAK, engine.Execute());
        CollectionAssert.AreEqual(new[] { 2 }, hits);

        debugger.Continue();
        Assert.AreEqual(VMState.HALT, engine.Execute());
        Assert.AreEqual(3, engine.ResultStack.Pop().GetInteger());
    }

    [TestMethod]
    public void TestStepMode()
    {
        using ExecutionEngine engine = new();
        using ScriptBuilder script = new();
        script.Emit(OpCode.NOP);
        script.Emit(OpCode.NOP);
        script.Emit(OpCode.RET);

        engine.LoadScript(script.ToArray());

        DebuggerMiddleware debugger = new(engine)
        {
            StepMode = true
        };

        Assert.AreEqual(VMState.BREAK, engine.Execute());
        Assert.AreEqual(1, engine.CurrentContext!.InstructionPointer);

        debugger.Continue();
        Assert.AreEqual(VMState.BREAK, engine.Execute());
        Assert.AreEqual(2, engine.CurrentContext!.InstructionPointer);
    }

    [TestMethod]
    public void TestAddBreakpointWithoutContextThrows()
    {
        using ExecutionEngine engine = new();
        DebuggerMiddleware debugger = new(engine);
        Assert.ThrowsExactly<InvalidOperationException>(() => debugger.AddBreakpoint(0));
    }

    [TestMethod]
    public void TestNullEngineThrows()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new DebuggerMiddleware(null!));
    }

    [TestMethod]
    public void TestDebuggerWithLoggerLogsStartAndBreak()
    {
        var logger = new CollectingLogger { MinLevel = LogLevel.Debug };
        using ExecutionEngine engine = new();
        using ScriptBuilder script = new();
        script.Emit(OpCode.NOP);
        script.Emit(OpCode.NOP);
        script.Emit(OpCode.RET);
        engine.LoadScript(script.ToArray());

        Debugger debugger = new(engine, logger);
        debugger.AddBreakPoint(engine.CurrentContext!.Script, 1);

        Assert.AreEqual(VMState.BREAK, debugger.Execute());
        Assert.IsTrue(logger.Messages.Exists(static m => m.Contains("Starting debugger") && m.Contains("L0001")));
        Assert.IsTrue(logger.Messages.Exists(static m => m.Contains("Breakpoint hit")));
        Assert.IsTrue(logger.Entries.Exists(static e => e.EventId.Id == VirtualMachineEventId.Break));
    }

    [TestMethod]
    public void TestStepIntoAndOverWhenHalted()
    {
        using ExecutionEngine engine = new();
        using ScriptBuilder script = new();
        script.Emit(OpCode.RET);
        engine.LoadScript(script.ToArray());

        DebuggerMiddleware debugger = new(engine);
        Assert.AreEqual(VMState.HALT, debugger.Execute());
        Assert.AreEqual(VMState.HALT, debugger.StepInto());
        Assert.AreEqual(VMState.HALT, debugger.StepOver());
    }

    [TestMethod]
    public void TestStepIntoAndOverWhenFaulted()
    {
        using ExecutionEngine engine = new();
        using ScriptBuilder script = new();
        script.Emit(OpCode.ABORT);
        engine.LoadScript(script.ToArray());

        DebuggerMiddleware debugger = new(engine);
        Assert.AreEqual(VMState.FAULT, debugger.Execute());
        Assert.AreEqual(VMState.FAULT, debugger.StepInto());
        Assert.AreEqual(VMState.FAULT, debugger.StepOver());
    }

    [TestMethod]
    public void TestStepOut()
    {
        using ExecutionEngine engine = new();
        using ScriptBuilder script = new();
        script.EmitCall(4);
        script.Emit(OpCode.NOP);
        script.Emit(OpCode.RET);
        script.Emit(OpCode.PUSH0);
        script.Emit(OpCode.RET);

        engine.LoadScript(script.ToArray());
        DebuggerMiddleware debugger = new(engine);

        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.HasCount(2, engine.InvocationStack);
        Assert.AreEqual(VMState.BREAK, debugger.StepOut());
        Assert.HasCount(1, engine.InvocationStack);
        Assert.AreEqual(OpCode.NOP, engine.CurrentContext!.CurrentInstruction!.OpCode);
    }

    [TestMethod]
    public void TestPostExecutionClearsStepMode()
    {
        using ExecutionEngine engine = new();
        using ScriptBuilder script = new();
        script.Emit(OpCode.RET);
        engine.LoadScript(script.ToArray());

        DebuggerMiddleware debugger = new(engine);
        Assert.AreEqual(VMState.HALT, debugger.Execute());
        debugger.StepMode = true;
        Assert.AreEqual(VMState.HALT, debugger.Execute());
        Assert.IsFalse(debugger.StepMode);
    }

    [TestMethod]
    public void TestBreakpointOnOtherScriptIsIgnored()
    {
        using ExecutionEngine engine = new();
        using ScriptBuilder script = new();
        script.Emit(OpCode.NOP);
        script.Emit(OpCode.RET);
        engine.LoadScript(script.ToArray());

        DebuggerMiddleware debugger = new(engine);
        debugger.AddBreakPoint(new Script(new byte[] { (byte)OpCode.RET }), 0);
        Assert.AreEqual(VMState.HALT, engine.Execute());
    }

    [TestMethod]
    public void TestRemoveMissingBreakPoint()
    {
        using ExecutionEngine engine = new();
        using ScriptBuilder script = new();
        script.Emit(OpCode.RET);
        engine.LoadScript(script.ToArray());

        DebuggerMiddleware debugger = new(engine);
        var loaded = engine.CurrentContext!.Script;
        Assert.IsFalse(debugger.RemoveBreakPoint(new Script(new byte[] { (byte)OpCode.NOP }), 0));
        debugger.AddBreakPoint(loaded, 0);
        Assert.IsFalse(debugger.RemoveBreakPoint(loaded, 1));
        Assert.IsTrue(debugger.RemoveBreakPoint(loaded, 0));
        Assert.IsFalse(debugger.RemoveBreakPoint(loaded, 0));
    }
}
