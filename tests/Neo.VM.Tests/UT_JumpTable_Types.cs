// Copyright (C) 2015-2026 The Neo Project.
//
// UT_JumpTable_Types.cs file belongs to the neo project and is free
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

namespace Neo.Test;

[TestClass]
public class UT_JumpTable_Types
{
    private class StatsCapturingEngine : ExecutionEngine
    {
        public RunStats? AssertMsgStats;

        protected override void PostExecuteInstruction(Instruction? instruction, RunStats runStats)
        {
            if (instruction?.OpCode == OpCode.ASSERTMSG)
                AssertMsgStats = runStats;
            base.PostExecuteInstruction(instruction, runStats);
        }
    }

    private static int RunAssertMsgAndGetLength(string message)
    {
        using var engine = new StatsCapturingEngine();
        using var sb = new ScriptBuilder();
        sb.EmitPush(true);
        sb.EmitPush(message);
        sb.Emit(OpCode.ASSERTMSG);
        engine.LoadScript(sb.ToArray());
        engine.Execute();

        Assert.AreEqual(VMState.HALT, engine.State);
        Assert.IsNotNull(engine.AssertMsgStats);
        return engine.AssertMsgStats!.Value.Length;
    }

    [TestMethod]
    public void AssertMsg_Ascii_LengthIsByteCount()
    {
        Assert.AreEqual(4, RunAssertMsgAndGetLength("FAIL"));
    }

    [TestMethod]
    public void AssertMsg_AccentedChar_LengthIsUtf8ByteCount()
    {
        // "é" (U+00E9) is 1 UTF-16 code unit but 2 bytes in UTF-8.
        Assert.AreEqual(2, RunAssertMsgAndGetLength("é"));
    }

    [TestMethod]
    public void AssertMsg_Emoji_LengthIsUtf8ByteCount()
    {
        // 😀 (U+1F600) is a UTF-16 surrogate pair (2 code units) but 4 bytes in UTF-8.
        Assert.AreEqual(4, RunAssertMsgAndGetLength("😀"));
    }

    [TestMethod]
    public void AssertMsg_MixedNonAscii_LengthIsUtf8ByteCount()
    {
        // "é" (2 bytes) + "😀" (4 bytes) + "!" (1 byte) = 7 bytes,
        // while string.Length would report 1 + 2 + 1 = 4.
        Assert.AreEqual(7, RunAssertMsgAndGetLength("é😀!"));
    }
}
