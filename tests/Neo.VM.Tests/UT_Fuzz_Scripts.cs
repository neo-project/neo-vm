// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Fuzz_Scripts.cs file belongs to the neo project and is free
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

namespace Neo.Test;

[TestClass]
[TestCategory("Nightly")]
public class UT_Fuzz_Scripts
{
    [TestMethod]
    public void ExecuteVsDebugger_FuzzedScripts_Match()
    {
        foreach (var bytes in ScriptFuzzer.GenerateScripts(seed: 1234, scriptCount: 50, maxLength: 64))
        {
            using var engineExecute = new TestEngine();
            using var engineDebug = new TestEngine();
            engineExecute.LoadScript(bytes);
            engineDebug.LoadScript(bytes);

            var executeSnapshot = ExecutionTestHelpers.RunToCompletion(engineExecute, useDebugger: false, maxSteps: 10_000);
            var debugSnapshot = ExecutionTestHelpers.RunToCompletion(engineDebug, useDebugger: true, maxSteps: 10_000);
            ExecutionTestHelpers.AssertSnapshotsEqual(executeSnapshot, debugSnapshot);
        }
    }
}
