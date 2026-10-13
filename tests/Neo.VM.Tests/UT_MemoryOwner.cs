// Copyright (C) 2015-2026 The Neo Project.
//
// UT_MemoryOwner.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using Neo.VM.Types;
using System;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.Test;

[TestClass]
public class UT_MemoryOwner
{
    [TestMethod]
    public void ByteString_CopiesIntoPooledMemory()
    {
        byte[] data = [1, 2, 3];
        var item = new ByteString(data);
        try
        {
            Assert.IsTrue(item.HasOwner);
            data[0] = 9;
            byte[] expected = [1, 2, 3];
            CollectionAssert.AreEqual(expected, item.GetSpan().ToArray());
        }
        finally
        {
            item.Cleanup();
        }
    }

    [TestMethod]
    public void ByteString_FromImmutable_Aliases()
    {
        byte[] data = [1, 2, 3];
        var item = ByteString.FromImmutable(data);
        Assert.IsFalse(item.HasOwner);
        data[0] = 9;
        Assert.AreEqual(9, item.GetSpan()[0]);
        item.Cleanup();
        Assert.AreEqual(9, item.GetSpan()[0]);
    }

    [TestMethod]
    public void ByteString_Empty_HasNoOwner()
    {
        Assert.IsFalse(ByteString.Empty.HasOwner);
        Assert.AreEqual(0, ByteString.Empty.Size);
        ByteString.Empty.Cleanup();
        ByteString.Empty.Cleanup();
        Assert.AreEqual(0, ByteString.Empty.GetSpan().Length);
    }

    [TestMethod]
    public void ByteString_KeepAlive_DoesNotDispose()
    {
        byte[] data = [7, 8];
        var item = new ByteString(data);
        item.KeepAlive();
        item.Cleanup();
        Assert.AreEqual(7, item.GetSpan()[0]);
        item.GetSpan().ToArray()[0] = 0;
        Assert.AreEqual(7, item.GetSpan()[0]);
    }

    [TestMethod]
    public void ByteString_DoubleCleanup_IsIdempotent()
    {
        byte[] data = [1];
        var item = new ByteString(data);
        item.Cleanup();
        item.Cleanup();
        Assert.IsFalse(item.HasOwner);
    }

    [TestMethod]
    public void ByteString_Drop_ReturnsOwner()
    {
        using var engine = new ExecutionEngine();
        byte[] script = [(byte)OpCode.DROP, (byte)OpCode.RET];
        engine.LoadScript(script);
        byte[] data = [1, 2, 3];
        var item = new ByteString(data);
        engine.Push(item);
        Assert.IsTrue(item.HasOwner);
        Assert.AreEqual(VMState.HALT, engine.Execute());
        Assert.IsFalse(item.HasOwner);
    }

    [TestMethod]
    public void ByteString_Pop_KeepAlive()
    {
        using var engine = new ExecutionEngine();
        byte[] script = [(byte)OpCode.NOP, (byte)OpCode.RET];
        engine.LoadScript(script);
        byte[] data = [1, 2, 3];
        var item = new ByteString(data);
        engine.Push(item);
        var popped = engine.Pop();
        Assert.AreSame(item, popped);
        Assert.IsTrue(item.HasOwner);
        CollectionAssert.AreEqual(data, item.GetSpan().ToArray());
    }

    [TestMethod]
    public void PushData_UsesImmutableAlias()
    {
        byte[] payload = [9, 9, 9];
        using var engine = new ExecutionEngine();
        using var sb = new ScriptBuilder();
        sb.EmitPush(payload);
        sb.Emit(OpCode.RET);
        engine.LoadScript(sb.ToArray());
        Assert.AreEqual(VMState.HALT, engine.Execute());
        var item = (ByteString)engine.ResultStack.Pop();
        Assert.IsFalse(item.HasOwner);
        CollectionAssert.AreEqual(payload, item.GetSpan().ToArray());
    }

    [TestMethod]
    public void Buffer_FollowsMasterMemoryStream()
    {
        var buffer = new Buffer(4);
        Assert.AreEqual(4, buffer.Size);
        byte[] zeros = [0, 0, 0, 0];
        CollectionAssert.AreEqual(zeros, buffer.GetSpan().ToArray());

        byte[] data = [1, 2, 3];
        var copied = new Buffer(data);
        data[0] = 9;
        byte[] copiedExpected = [1, 2, 3];
        CollectionAssert.AreEqual(copiedExpected, copied.GetSpan().ToArray());
    }
}
