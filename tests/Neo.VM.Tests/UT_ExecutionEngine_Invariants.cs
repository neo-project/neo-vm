// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ExecutionEngine_Invariants.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Test.Types;
using Neo.VM;

namespace Neo.Test;

[TestClass]
public class UT_ExecutionEngine_Invariants
{
    [TestMethod]
    public void Execute_OnBreak_ResetsStateAndRuns()
    {
        using var engine = new TestEngine();
        using var script = new ScriptBuilder();
        script.Emit(OpCode.NOP);
        script.Emit(OpCode.RET);
        engine.LoadScript(script.ToArray());

        engine.State = VMState.BREAK;
        engine.Execute();

        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.IsNull(engine.CurrentContext);
    }

    [TestMethod]
    public void Execute_WithNoInvocationStack_Halts()
    {
        using var engine = new TestEngine();
        engine.Execute();
        Assert.AreEqual(VMState.HALT, engine.State);
    }
}
