// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Limits.cs file belongs to the neo project and is free
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
public class UT_Limits
{
    [TestMethod]
    public void StackLimit_OverflowFaults()
    {
        using var engine = new TestEngine();
        using var script = new ScriptBuilder();

        for (int i = 0; i < engine.Limits.MaxStackSize + 1; i++)
            script.Emit(OpCode.PUSH0);
        script.Emit(OpCode.RET);

        engine.LoadScript(script.ToArray());
        engine.Execute();

        Assert.AreEqual(VMState.FAULT, engine.State);
    }
}
