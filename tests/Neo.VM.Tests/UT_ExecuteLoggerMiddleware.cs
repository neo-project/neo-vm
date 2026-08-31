// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ExecuteLoggerMiddleware.cs file belongs to the neo project and is free
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
using Neo.VM.Extensions;
using Neo.VM.Logging;
using Neo.VM.Middleware;
using System;

namespace Neo.Test;

[TestClass]
public class UT_ExecuteLoggerMiddleware
{
    [TestMethod]
    public void TestLogsStartOpcodesAndFinish()
    {
        var logger = new CollectingLogger();
        using var engine = new ExecutionEngine();
        engine.Use(new ExecuteLoggerMiddleware(engine, logger));

        using ScriptBuilder sb = new();
        sb.Emit(OpCode.NOP);
        sb.Emit(OpCode.RET);
        engine.LoadScript(sb.ToArray());

        Assert.AreEqual(VMState.HALT, engine.Execute());

        Assert.IsTrue(logger.Messages.Exists(static m => m.Contains("VM execution starting")));
        Assert.IsTrue(logger.Messages.Exists(static m => m.Contains("NOP")));
        Assert.IsTrue(logger.Messages.Exists(static m => m.Contains("VM execution finished") && m.Contains("HALT")));
        Assert.IsTrue(logger.Entries.Exists(static e => e.EventId.Id == VirtualMachineEventId.Execute));
    }

    [TestMethod]
    public void TestLogsOpcodeOperand()
    {
        var logger = new CollectingLogger();
        using var engine = new ExecutionEngine();
        engine.Use(new ExecuteLoggerMiddleware(engine, logger));

        using ScriptBuilder sb = new();
        sb.Emit(OpCode.PUSHINT8, [0xAB]);
        sb.Emit(OpCode.RET);
        engine.LoadScript(sb.ToArray());

        Assert.AreEqual(VMState.HALT, engine.Execute());
        Assert.IsTrue(logger.Messages.Exists(static m => m.Contains("PUSHINT8") && m.Contains("AB", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void TestLogsFault()
    {
        var logger = new CollectingLogger();
        using var engine = new ExecutionEngine();
        engine.Use(new ExecuteLoggerMiddleware(engine, logger));

        using ScriptBuilder sb = new();
        sb.Emit(OpCode.ABORT);
        engine.LoadScript(sb.ToArray());

        Assert.AreEqual(VMState.FAULT, engine.Execute());
        Assert.IsNotNull(engine.FaultException);
        Assert.IsTrue(logger.Messages.Exists(static m => m.Contains("VM execution finished") && m.Contains("FAULT")));
        Assert.IsTrue(logger.Entries.Exists(static e => e.EventId.Id == VirtualMachineEventId.Fault && e.Exception is not null));

        logger.Messages.Clear();
        logger.Entries.Clear();
        Assert.AreEqual(VMState.FAULT, engine.Execute());
        Assert.IsTrue(logger.Messages.Exists(static m => m.Contains("VM execution starting") && m.Contains("FAULT")));
        Assert.IsTrue(logger.Entries.Exists(static e => e.Level == LogLevel.Critical && e.Exception is not null));
    }

    [TestMethod]
    public void TestNullLoggerDoesNotThrow()
    {
        using var engine = new ExecutionEngine();
        engine.Use(new ExecuteLoggerMiddleware(engine));

        using ScriptBuilder sb = new();
        sb.Emit(OpCode.RET);
        engine.LoadScript(sb.ToArray());
        Assert.AreEqual(VMState.HALT, engine.Execute());
    }

    [TestMethod]
    public void TestNullEngineThrows()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new ExecuteLoggerMiddleware(null!));
    }

    [TestMethod]
    public void TestDisabledLoggerSkipsWrites()
    {
        var logger = new CollectingLogger { MinLevel = LogLevel.None };
        using var engine = new ExecutionEngine();
        engine.Use(new ExecuteLoggerMiddleware(engine, logger));

        using ScriptBuilder sb = new();
        sb.Emit(OpCode.RET);
        engine.LoadScript(sb.ToArray());
        Assert.AreEqual(VMState.HALT, engine.Execute());
        Assert.HasCount(0, logger.Messages);
    }

    [TestMethod]
    public void TestLoggerExtensions()
    {
        var logger = new CollectingLogger();
        var error = new InvalidOperationException("boom");

        logger.LogFaultMessage(LogLevel.Error, "fault-text");
        logger.LogFaultMessage(LogLevel.Error, error, "fault-ex");
        logger.LogCreateMessage(LogLevel.Information, "create");
        logger.LogLoadMessage(LogLevel.Information, "load");
        logger.LogPrePostMessage(LogLevel.Debug, "prepost");
        logger.LogPostMessage(LogLevel.Debug, "post");
        logger.LogBreakMessage(LogLevel.Debug, "break");
        logger.LogExecuteMessage(LogLevel.Information, "exec");
        logger.LogBurnMessage(LogLevel.Trace, "burn");
        logger.LogCallMessage(LogLevel.Trace, "call");
        logger.LogNotifyMessage(LogLevel.Information, "notify");
        logger.LogLogMessage(LogLevel.Information, "log");
        logger.LogPersistMessage(LogLevel.Information, "persist");
        logger.LogPostPersistMessage(LogLevel.Information, "postpersist");
        logger.LogStoragePutMessage(LogLevel.Trace, "sput");
        logger.LogStorageGetMessage(LogLevel.Trace, "sget");
        logger.LogStorageFindMessage(LogLevel.Trace, "sfind");
        logger.LogStorageDeleteMessage(LogLevel.Trace, "sdel");
        logger.LogIteratorNextMessage(LogLevel.Trace, "inext");
        logger.LogIteratorGetMessage(LogLevel.Trace, "iget");
        logger.LogReadStorageMessage(LogLevel.Trace, "read");
        logger.LogUpdateStorageMessage(LogLevel.Trace, "update");

        Assert.HasCount(22, logger.Entries);

        AssertEntry(logger, 0, VirtualMachineEventId.Fault, "fault-text", nameof(VirtualMachineEventId.Fault));
        Assert.AreEqual(VirtualMachineEventId.Fault, logger.Entries[1].EventId.Id);
        Assert.AreEqual("fault-ex", logger.Entries[1].Message);
        Assert.AreEqual("FaultException", logger.Entries[1].EventId.Name);
        Assert.AreSame(error, logger.Entries[1].Exception);

        AssertEntry(logger, 2, VirtualMachineEventId.Create, "create");
        AssertEntry(logger, 3, VirtualMachineEventId.Load, "load");
        AssertEntry(logger, 4, VirtualMachineEventId.PrePost, "prepost");
        AssertEntry(logger, 5, VirtualMachineEventId.Post, "post");
        AssertEntry(logger, 6, VirtualMachineEventId.Break, "break");
        AssertEntry(logger, 7, VirtualMachineEventId.Execute, "exec");
        AssertEntry(logger, 8, VirtualMachineEventId.Burn, "burn");
        AssertEntry(logger, 9, VirtualMachineEventId.Call, "call");
        AssertEntry(logger, 10, VirtualMachineEventId.Notify, "notify");
        AssertEntry(logger, 11, VirtualMachineEventId.Log, "log");
        AssertEntry(logger, 12, VirtualMachineEventId.Persist, "persist");
        AssertEntry(logger, 13, VirtualMachineEventId.PostPersist, "postpersist");
        AssertEntry(logger, 14, VirtualMachineEventId.StoragePut, "sput");
        AssertEntry(logger, 15, VirtualMachineEventId.StorageGet, "sget");
        AssertEntry(logger, 16, VirtualMachineEventId.StorageFind, "sfind");
        AssertEntry(logger, 17, VirtualMachineEventId.StorageDelete, "sdel");
        AssertEntry(logger, 18, VirtualMachineEventId.IteratorNext, "inext");
        AssertEntry(logger, 19, VirtualMachineEventId.IteratorGet, "iget");
        AssertEntry(logger, 20, VirtualMachineEventId.ReadStorage, "read");
        AssertEntry(logger, 21, VirtualMachineEventId.UpdateStorage, "update");
    }

    private static void AssertEntry(CollectingLogger logger, int index, int eventId, string message, string? eventName = null)
    {
        var entry = logger.Entries[index];
        Assert.AreEqual(eventId, entry.EventId.Id, message);
        Assert.AreEqual(message, entry.Message);
        Assert.IsNull(entry.Exception);
        if (eventName is not null)
            Assert.AreEqual(eventName, entry.EventId.Name);
    }
}
