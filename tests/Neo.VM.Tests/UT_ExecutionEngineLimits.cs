// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ExecutionEngineLimits.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using System;

namespace Neo.VM.Tests;

[TestClass]
public class UT_ExecutionEngineLimits
{
    [TestMethod]
    public void TestCustomInvocationStackLimit()
    {
        var limits = ExecutionEngineLimits.Default with { MaxInvocationStackSize = 2 };
        using var engine = new ExecutionEngine(null, limits);
        Assert.AreSame(limits, engine.Limits);

        var context = engine.LoadScript(new byte[] { (byte)OpCode.RET });
        engine.LoadContext(context.Clone());
        Assert.ThrowsExactly<InvalidOperationException>(() => engine.LoadContext(context.Clone()));
        Assert.HasCount(2, engine.InvocationStack);
    }

    [TestMethod]
    public void TestDefaultInvocationStackLimitUnchanged()
    {
        using var engine = new ExecutionEngine();
        Assert.AreEqual(1024u, engine.Limits.MaxInvocationStackSize);

        var context = engine.LoadScript(new byte[] { (byte)OpCode.RET });
        engine.LoadContext(context.Clone());
        engine.LoadContext(context.Clone());
        Assert.HasCount(3, engine.InvocationStack);
    }
}
