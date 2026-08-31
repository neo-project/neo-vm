// Copyright (C) 2015-2026 The Neo Project.
//
// UT_EngineMiddleware.cs file belongs to the neo project and is free
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
using Neo.VM.Middleware;
using System.Collections.Generic;

namespace Neo.Test;

[TestClass]
public class UT_EngineMiddleware
{
    [TestMethod]
    public void TestPipelineOrder()
    {
        var events = new List<string>();
        using var engine = new ExecutionEngine();
        engine.Use(new RecordingMiddleware("A", events));
        engine.Use(new RecordingMiddleware("B", events));

        using ScriptBuilder sb = new();
        sb.Emit(OpCode.NOP);
        sb.Emit(OpCode.RET);
        engine.LoadScript(sb.ToArray());

        Assert.AreEqual(VMState.HALT, engine.Execute());
        CollectionAssert.AreEqual(new[]
        {
            "A.PreExecution",
            "B.PreExecution",
            "A.PreExecute",
            "B.PreExecute",
            "A.PostExecute",
            "B.PostExecute",
            "A.PreExecute",
            "B.PreExecute",
            "A.PostExecute",
            "B.PostExecute",
            "A.PostExecution",
            "B.PostExecution",
        }, events);
    }

    [TestMethod]
    public void TestUseIgnoresDuplicates()
    {
        var events = new List<string>();
        var middleware = new RecordingMiddleware("A", events);
        using var engine = new ExecutionEngine();
        engine.Use(middleware);
        engine.Use(middleware);

        using ScriptBuilder sb = new();
        sb.Emit(OpCode.RET);
        engine.LoadScript(sb.ToArray());
        Assert.AreEqual(VMState.HALT, engine.Execute());

        Assert.HasCount(1, events.FindAll(static e => e == "A.PreExecution"));
    }

    [TestMethod]
    public void TestNotCallingNextSkipsLaterPreExecute()
    {
        var events = new List<string>();
        using var engine = new ExecutionEngine();
        engine.Use(new BlockingMiddleware());
        engine.Use(new RecordingMiddleware("B", events));

        using ScriptBuilder sb = new();
        sb.Emit(OpCode.PUSH1);
        sb.Emit(OpCode.RET);
        engine.LoadScript(sb.ToArray());

        Assert.AreEqual(VMState.HALT, engine.Execute());
        Assert.AreEqual(1, engine.ResultStack.Count);
        CollectionAssert.AreEqual(new[]
        {
            "B.PreExecution",
            "B.PostExecute",
            "B.PostExecute",
            "B.PostExecution",
        }, events);
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

    private sealed class BlockingMiddleware : IEngineMiddleware
    {
        public void PreExecute(ExecutionContext? context, ExecuteDelegate next)
        {
        }
    }
}
