// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ExecutionBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using Neo.VM.Builder;
using Neo.VM.Logging;
using Neo.VM.Middleware;
using Neo.VM.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Neo.Test;

[TestClass]
public class UT_ExecutionBuilder
{
    [TestMethod]
    public void TestPipelineBuilderEmptyInvokesNoOps()
    {
        var pipeline = ExecutionPipelineBuilder.Create().Build();
        pipeline.PreExecution();
        pipeline.PostExecution();
        pipeline.PreExecute(null);
        pipeline.PostExecute(null);
    }

    [TestMethod]
    public void TestPipelineBuilderUseNullThrows()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => ExecutionPipelineBuilder.Create().Use((IEngineMiddleware)null!));
    }

    [TestMethod]
    public void TestPipelineBuilderUseMiddlewareType()
    {
        var pipeline = ExecutionPipelineBuilder.Create()
            .UseMiddleware<NoopPublicMiddleware>()
            .Build();

        using var engine = ExecutionEngineBuilder.Create()
            .UsePipeline(pipeline)
            .Build();

        using ScriptBuilder sb = new();
        sb.Emit(OpCode.RET);
        engine.LoadScript(sb.ToArray());
        Assert.AreEqual(VMState.HALT, engine.Execute());
    }

    [TestMethod]
    public void TestPipelineBuilderUseEnumerable()
    {
        var events = new List<string>();
        var pipeline = ExecutionPipelineBuilder.Create()
            .Use(new IEngineMiddleware[] { new RecordingMiddleware("B", events) })
            .Build();

        using var engine = ExecutionEngineBuilder.Create()
            .UsePipeline(pipeline)
            .Build();

        using ScriptBuilder sb = new();
        sb.Emit(OpCode.RET);
        engine.LoadScript(sb.ToArray());
        Assert.AreEqual(VMState.HALT, engine.Execute());
        CollectionAssert.AreEqual(new[]
        {
            "B.PreExecution",
            "B.PreExecute",
            "B.PostExecute",
            "B.PostExecution",
        }, events);
    }

    [TestMethod]
    public void TestEngineBuilderDefaultsAndOptions()
    {
        var jumpTable = new JumpTable();
        var limits = new ExecutionEngineLimits { MaxStackSize = 16 };
        var referenceCounter = new ReferenceCounter(limits);

        using var engine = ExecutionEngineBuilder.Create()
            .UseJumpTable(jumpTable)
            .UseLimits(limits)
            .UseReferenceCounter(referenceCounter)
            .Build();

        Assert.AreSame(jumpTable, engine.JumpTable);
        Assert.AreEqual(16u, engine.Limits.MaxStackSize);
        Assert.AreSame(referenceCounter, engine.ReferenceCounter);
        Assert.AreSame(ExecutionPipeline.Empty, engine.Pipeline);
    }

    [TestMethod]
    public void TestEngineBuilderUsePipelineAction()
    {
        var events = new List<string>();
        using var engine = ExecutionEngineBuilder.Create()
            .UsePipeline(p => p.Use(new RecordingMiddleware("A", events)))
            .Build();

        using ScriptBuilder sb = new();
        sb.Emit(OpCode.RET);
        engine.LoadScript(sb.ToArray());
        Assert.AreEqual(VMState.HALT, engine.Execute());
        CollectionAssert.Contains(events, "A.PreExecution");
        CollectionAssert.Contains(events, "A.PostExecution");
    }

    [TestMethod]
    public void TestUseNullMiddlewareThrows()
    {
        using var engine = new ExecutionEngine();
        Assert.ThrowsExactly<ArgumentNullException>(() => engine.Use(null!));
    }

    [TestMethod]
    public void TestExecuteWithoutScriptHalts()
    {
        using var engine = new ExecutionEngine();
        Assert.AreEqual(VMState.HALT, engine.Execute());
    }

    [TestMethod]
    public void TestDefaultMiddlewareInterfaceMethods()
    {
        using var engine = new ExecutionEngine();
        engine.Use(new DefaultMiddleware());
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.PUSH1);
        sb.Emit(OpCode.RET);
        engine.LoadScript(sb.ToArray());
        Assert.AreEqual(VMState.HALT, engine.Execute());
        Assert.AreEqual(1, engine.ResultStack.Pop().GetInteger());
    }

    [TestMethod]
    public void TestVirtualMachineEventIds()
    {
        var ids = typeof(VirtualMachineEventId)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(static f => f.IsLiteral)
            .ToDictionary(static f => f.Name, static f => (int)f.GetRawConstantValue()!);

        Assert.HasCount(21, ids);
        Assert.AreEqual(100, ids["Fault"]);
        Assert.AreEqual(205, ids["Execute"]);
        Assert.AreEqual(204, ids["Break"]);
        Assert.AreEqual(701, ids["UpdateStorage"]);
    }

    public sealed class NoopPublicMiddleware : IEngineMiddleware
    {
    }

    private sealed class DefaultMiddleware : IEngineMiddleware
    {
    }

    private sealed class RecordingMiddleware(string name, List<string> events) : IEngineMiddleware
    {
        public void PreExecution(ExecutionDelegate next)
        {
            events.Add($"{name}.PreExecution");
            next();
        }

        public void PostExecution(ExecutionDelegate next)
        {
            events.Add($"{name}.PostExecution");
            next();
        }

        public void PreExecute(ExecutionContext? context, ExecuteDelegate next)
        {
            events.Add($"{name}.PreExecute");
            next(context);
        }

        public void PostExecute(ExecutionContext? context, ExecuteDelegate next)
        {
            events.Add($"{name}.PostExecute");
            next(context);
        }
    }
}
