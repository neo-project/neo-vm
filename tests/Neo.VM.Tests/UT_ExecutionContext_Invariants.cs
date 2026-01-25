// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ExecutionContext_Invariants.cs file belongs to the neo project and is free
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

namespace Neo.Test;

[TestClass]
public class UT_ExecutionContext_Invariants
{
    [TestMethod]
    public void MoveNext_StopsAtEndOfScript()
    {
        var scriptBytes = new byte[] { (byte)OpCode.NOP };
        var script = new Script(scriptBytes);
        var context = new ExecutionContext(script, 0, new ReferenceCounter());

        Assert.IsFalse(context.MoveNext());
        Assert.AreEqual(script.Length, context.InstructionPointer);
        Assert.IsNull(context.CurrentInstruction);
    }
}
