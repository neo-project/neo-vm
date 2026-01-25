// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ExecuteVsDebugger.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Test.Helpers;
using Neo.Test.Types;
using Neo.VM;

namespace Neo.Test;

[TestClass]
public class UT_ExecuteVsDebugger
{
    [TestMethod]
    public void ExecuteVsDebugger_BasicArithmetic_Matches()
    {
        using var engineExecute = new TestEngine();
        using var engineDebug = new TestEngine();
        using var script = new ScriptBuilder();
        script.Emit(OpCode.PUSH1);
        script.Emit(OpCode.PUSH2);
        script.Emit(OpCode.ADD);
        script.Emit(OpCode.RET);

        var bytes = script.ToArray();
        engineExecute.LoadScript(bytes);
        engineDebug.LoadScript(bytes);

        var executeSnapshot = ExecutionTestHelpers.RunToCompletion(engineExecute, useDebugger: false);
        var debugSnapshot = ExecutionTestHelpers.RunToCompletion(engineDebug, useDebugger: true);

        ExecutionTestHelpers.AssertSnapshotsEqual(executeSnapshot, debugSnapshot);
    }
}
