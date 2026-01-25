# VM Test Expansion Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Expand VM unit test coverage (engine invariants, execution equivalence, limits, and deterministic fuzz) while keeping all JSON-based tests running in the fast suite.

**Architecture:** Add shared helpers for execution snapshots, then build fast-path tests for invariants and execute-vs-debugger equivalence. Add deterministic, bounded fuzz tests labeled as Nightly. Keep JSON tests unchanged and always run in the fast suite.

**Tech Stack:** C#/.NET, MSTest, Neo.VM

### Task 1: Add execution snapshot helpers and a minimal equivalence test

**Files:**
- Create: `tests/Neo.VM.Tests/Helpers/ExecutionTestHelpers.cs`
- Create: `tests/Neo.VM.Tests/UT_ExecuteVsDebugger.cs`

**Step 1: Write the failing test**

Create `tests/Neo.VM.Tests/UT_ExecuteVsDebugger.cs`:

```csharp
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
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/Neo.VM.Tests/Neo.VM.Tests.csproj --filter ExecuteVsDebugger_BasicArithmetic_Matches`

Expected: FAIL because `ExecutionTestHelpers` does not exist.

**Step 3: Write minimal implementation**

Create `tests/Neo.VM.Tests/Helpers/ExecutionTestHelpers.cs`:

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using Neo.VM.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using VMArray = Neo.VM.Types.Array;

namespace Neo.Test.Helpers;

public static class ExecutionTestHelpers
{
    public sealed record FrameSnapshot(int InstructionPointer, OpCode NextInstruction, JToken[] EvaluationStack, JToken[] Arguments, JToken[] LocalVariables, JToken[] StaticFields);
    public sealed record EngineSnapshot(VMState State, JToken[] ResultStack, FrameSnapshot[] InvocationStack);

    public static EngineSnapshot RunToCompletion(ExecutionEngine engine, bool useDebugger, int maxSteps = 100_000)
    {
        if (!useDebugger)
        {
            engine.Execute();
            return CaptureSnapshot(engine);
        }

        var debugger = new Debugger(engine);
        var steps = 0;
        while (engine.State != VMState.HALT && engine.State != VMState.FAULT)
        {
            if (steps++ >= maxSteps)
                Assert.Fail($"Exceeded max steps: {maxSteps}");
            debugger.StepInto();
        }
        return CaptureSnapshot(engine);
    }

    public static void AssertSnapshotsEqual(EngineSnapshot expected, EngineSnapshot actual)
    {
        Assert.AreEqual(expected.State, actual.State);
        AssertTokensEqual(expected.ResultStack, actual.ResultStack, "ResultStack");
        Assert.AreEqual(expected.InvocationStack.Length, actual.InvocationStack.Length, "InvocationStack length mismatch");
        for (int i = 0; i < expected.InvocationStack.Length; i++)
        {
            var left = expected.InvocationStack[i];
            var right = actual.InvocationStack[i];
            Assert.AreEqual(left.InstructionPointer, right.InstructionPointer, $"IP mismatch at frame {i}");
            Assert.AreEqual(left.NextInstruction, right.NextInstruction, $"NextInstruction mismatch at frame {i}");
            AssertTokensEqual(left.EvaluationStack, right.EvaluationStack, $"EvaluationStack mismatch at frame {i}");
            AssertTokensEqual(left.Arguments, right.Arguments, $"Arguments mismatch at frame {i}");
            AssertTokensEqual(left.LocalVariables, right.LocalVariables, $"Locals mismatch at frame {i}");
            AssertTokensEqual(left.StaticFields, right.StaticFields, $"Statics mismatch at frame {i}");
        }
    }

    private static EngineSnapshot CaptureSnapshot(ExecutionEngine engine)
    {
        var result = new List<JToken>();
        for (int i = 0; i < engine.ResultStack.Count; i++)
            result.Add(ItemToJson(engine.ResultStack.Peek(i)));

        var frames = new List<FrameSnapshot>();
        foreach (var context in engine.InvocationStack)
        {
            var next = context.InstructionPointer >= context.Script.Length ? OpCode.RET : context.Script[context.InstructionPointer];
            frames.Add(new FrameSnapshot(
                context.InstructionPointer,
                next,
                StackToJson(context.EvaluationStack),
                SlotToJson(context.Arguments),
                SlotToJson(context.LocalVariables),
                SlotToJson(context.StaticFields)));
        }

        return new EngineSnapshot(engine.State, result.ToArray(), frames.ToArray());
    }

    private static JToken[] StackToJson(EvaluationStack stack)
    {
        var items = new JToken[stack.Count];
        for (int i = 0; i < stack.Count; i++)
            items[i] = ItemToJson(stack.Peek(i));
        return items;
    }

    private static JToken[] SlotToJson(Slot slot)
    {
        if (slot == null) return new JToken[0];
        var items = new JToken[slot.Count];
        for (int i = 0; i < slot.Count; i++)
            items[i] = ItemToJson(slot[i]);
        return items;
    }

    private static JToken ItemToJson(StackItem item)
    {
        if (item is null) return JValue.CreateNull();
        return item switch
        {
            Null => new JObject { ["type"] = nameof(Null) },
            Pointer p => new JObject { ["type"] = nameof(Pointer), ["value"] = p.Position },
            Boolean b => new JObject { ["type"] = nameof(Boolean), ["value"] = b.GetBoolean() },
            Integer i => new JObject { ["type"] = nameof(Integer), ["value"] = i.GetInteger().ToString() },
            ByteString s => new JObject { ["type"] = nameof(ByteString), ["value"] = s.GetSpan().ToArray() },
            Buffer b => new JObject { ["type"] = nameof(Buffer), ["value"] = b.GetSpan().ToArray() },
            Struct st => new JObject { ["type"] = nameof(Struct), ["value"] = ItemsToJson(st) },
            VMArray a => new JObject { ["type"] = nameof(VMArray), ["value"] = ItemsToJson(a) },
            Map m => new JObject { ["type"] = nameof(Map), ["value"] = MapToJson(m) },
            _ => new JObject { ["type"] = item.GetType().Name, ["value"] = item.ToString() }
        };
    }

    private static JArray ItemsToJson(IReadOnlyList<StackItem> items)
    {
        var array = new JArray();
        for (int i = 0; i < items.Count; i++)
            array.Add(ItemToJson(items[i]));
        return array;
    }

    private static JObject MapToJson(Map map)
    {
        var obj = new JObject();
        foreach (var pair in map)
            obj[pair.Key.ToString()] = ItemToJson(pair.Value);
        return obj;
    }

    private static void AssertTokensEqual(JToken[] left, JToken[] right, string label)
    {
        Assert.AreEqual(left.Length, right.Length, label + " length mismatch");
        for (int i = 0; i < left.Length; i++)
            Assert.IsTrue(JToken.DeepEquals(left[i], right[i]), $"{label} item mismatch at {i}");
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/Neo.VM.Tests/Neo.VM.Tests.csproj --filter ExecuteVsDebugger_BasicArithmetic_Matches`

Expected: PASS

**Step 5: Commit**

```bash
git add tests/Neo.VM.Tests/Helpers/ExecutionTestHelpers.cs tests/Neo.VM.Tests/UT_ExecuteVsDebugger.cs
git commit -m "test: add execute vs debugger snapshot helper"
```

### Task 2: Expand execute-vs-debugger equivalence coverage (fast)

**Files:**
- Modify: `tests/Neo.VM.Tests/UT_ExecuteVsDebugger.cs`

**Step 1: Write the failing test**

Add a data-driven test with a curated script set:

```csharp
[TestMethod]
public void ExecuteVsDebugger_CuratedScripts_Match()
{
    foreach (var bytes in CuratedScripts())
    {
        using var engineExecute = new TestEngine();
        using var engineDebug = new TestEngine();
        engineExecute.LoadScript(bytes);
        engineDebug.LoadScript(bytes);

        var executeSnapshot = ExecutionTestHelpers.RunToCompletion(engineExecute, useDebugger: false);
        var debugSnapshot = ExecutionTestHelpers.RunToCompletion(engineDebug, useDebugger: true);
        ExecutionTestHelpers.AssertSnapshotsEqual(executeSnapshot, debugSnapshot);
    }
}

private static IEnumerable<byte[]> CuratedScripts()
{
    using var script = new ScriptBuilder();
    script.Emit(OpCode.PUSH0);
    script.Emit(OpCode.PUSH1);
    script.Emit(OpCode.ADD);
    script.Emit(OpCode.RET);
    yield return script.ToArray();

    using var script2 = new ScriptBuilder();
    script2.Emit(OpCode.PUSH1);
    script2.Emit(OpCode.DUP);
    script2.Emit(OpCode.SWAP);
    script2.Emit(OpCode.DROP);
    script2.Emit(OpCode.RET);
    yield return script2.ToArray();

    using var script3 = new ScriptBuilder();
    script3.Emit(OpCode.NEWARRAY0);
    script3.Emit(OpCode.PUSH1);
    script3.Emit(OpCode.APPEND);
    script3.Emit(OpCode.SIZE);
    script3.Emit(OpCode.RET);
    yield return script3.ToArray();
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/Neo.VM.Tests/Neo.VM.Tests.csproj --filter ExecuteVsDebugger_CuratedScripts_Match`

Expected: FAIL (missing helper method or scripts not compiled).

**Step 3: Write minimal implementation**

Add `CuratedScripts()` into `UT_ExecuteVsDebugger.cs` and ensure required `using` statements are present.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/Neo.VM.Tests/Neo.VM.Tests.csproj --filter ExecuteVsDebugger_CuratedScripts_Match`

Expected: PASS

**Step 5: Commit**

```bash
git add tests/Neo.VM.Tests/UT_ExecuteVsDebugger.cs
git commit -m "test: expand execute vs debugger coverage"
```

### Task 3: Add engine and context invariants (fast)

**Files:**
- Create: `tests/Neo.VM.Tests/UT_ExecutionEngine_Invariants.cs`
- Create: `tests/Neo.VM.Tests/UT_ExecutionContext_Invariants.cs`

**Step 1: Write the failing tests**

Create `tests/Neo.VM.Tests/UT_ExecutionEngine_Invariants.cs`:

```csharp
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
```

Create `tests/Neo.VM.Tests/UT_ExecutionContext_Invariants.cs`:

```csharp
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

        Assert.IsTrue(context.MoveNext());
        Assert.IsFalse(context.MoveNext());
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Neo.VM.Tests/Neo.VM.Tests.csproj --filter FullyQualifiedName~UT_ExecutionEngine_Invariants`

Expected: FAIL if any assumptions are wrong.

**Step 3: Write minimal implementation**

Adjust tests to match actual behavior if needed (no production code changes expected).

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Neo.VM.Tests/Neo.VM.Tests.csproj --filter FullyQualifiedName~UT_ExecutionEngine_Invariants`

Expected: PASS

**Step 5: Commit**

```bash
git add tests/Neo.VM.Tests/UT_ExecutionEngine_Invariants.cs tests/Neo.VM.Tests/UT_ExecutionContext_Invariants.cs
git commit -m "test: add execution engine invariants"
```

### Task 4: Add limits and bounds tests (fast)

**Files:**
- Create: `tests/Neo.VM.Tests/UT_Limits.cs`

**Step 1: Write the failing tests**

Create `tests/Neo.VM.Tests/UT_Limits.cs`:

```csharp
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
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/Neo.VM.Tests/Neo.VM.Tests.csproj --filter StackLimit_OverflowFaults`

Expected: FAIL if fault is not triggered or state differs.

**Step 3: Write minimal implementation**

Adjust test to align with actual limits behavior (no production code changes expected).

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/Neo.VM.Tests/Neo.VM.Tests.csproj --filter StackLimit_OverflowFaults`

Expected: PASS

**Step 5: Commit**

```bash
git add tests/Neo.VM.Tests/UT_Limits.cs
git commit -m "test: add limits coverage"
```

### Task 5: Add deterministic randomized script tests (nightly)

**Files:**
- Create: `tests/Neo.VM.Tests/Helpers/ScriptFuzzer.cs`
- Create: `tests/Neo.VM.Tests/UT_Fuzz_Scripts.cs`

**Step 1: Write the failing test**

Create `tests/Neo.VM.Tests/UT_Fuzz_Scripts.cs`:

```csharp
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
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/Neo.VM.Tests/Neo.VM.Tests.csproj --filter TestCategory=Nightly`

Expected: FAIL because `ScriptFuzzer` is missing.

**Step 3: Write minimal implementation**

Create `tests/Neo.VM.Tests/Helpers/ScriptFuzzer.cs` with a deterministic generator:

```csharp
using Neo.VM;
using System;
using System.Collections.Generic;

namespace Neo.Test.Helpers;

public static class ScriptFuzzer
{
    private static readonly OpCode[] _ops = new[]
    {
        OpCode.NOP,
        OpCode.PUSH0,
        OpCode.PUSH1,
        OpCode.PUSH2,
        OpCode.ADD,
        OpCode.SUB,
        OpCode.MUL,
        OpCode.NOT,
        OpCode.DUP,
        OpCode.SWAP,
        OpCode.DROP
    };

    public static IEnumerable<byte[]> GenerateScripts(int seed, int scriptCount, int maxLength)
    {
        var rand = new Random(seed);
        for (int i = 0; i < scriptCount; i++)
        {
            using var builder = new ScriptBuilder();
            var length = rand.Next(1, maxLength + 1);
            for (int j = 0; j < length; j++)
            {
                var op = _ops[rand.Next(_ops.Length)];
                builder.Emit(op);
            }
            builder.Emit(OpCode.RET);
            yield return builder.ToArray();
        }
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/Neo.VM.Tests/Neo.VM.Tests.csproj --filter TestCategory=Nightly`

Expected: PASS

**Step 5: Commit**

```bash
git add tests/Neo.VM.Tests/Helpers/ScriptFuzzer.cs tests/Neo.VM.Tests/UT_Fuzz_Scripts.cs
git commit -m "test: add nightly fuzzed script coverage"
```

### Task 6: Full verification (fast + JSON)

**Files:**
- Modify: none

**Step 1: Run full fast test suite**

Run: `dotnet test tests/Neo.VM.Tests/Neo.VM.Tests.csproj`

Expected: PASS (JSON tests included)

**Step 2: Commit summary note**

No commit (verification only).
