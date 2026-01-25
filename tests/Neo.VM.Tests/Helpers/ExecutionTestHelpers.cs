// Copyright (C) 2015-2026 The Neo Project.
//
// ExecutionTestHelpers.cs file belongs to the neo project and is free
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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using VMArray = Neo.VM.Types.Array;
using VMBoolean = Neo.VM.Types.Boolean;
using VMBuffer = Neo.VM.Types.Buffer;

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
        Assert.HasCount(expected.InvocationStack.Length, actual.InvocationStack, "InvocationStack length mismatch");
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
            VMBoolean b => new JObject { ["type"] = nameof(VMBoolean), ["value"] = b.GetBoolean() },
            Integer i => new JObject { ["type"] = nameof(Integer), ["value"] = i.GetInteger().ToString() },
            ByteString s => new JObject { ["type"] = nameof(ByteString), ["value"] = s.GetSpan().ToArray() },
            VMBuffer b => new JObject { ["type"] = nameof(VMBuffer), ["value"] = b.GetSpan().ToArray() },
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
        Assert.HasCount(left.Length, right, label + " length mismatch");
        for (int i = 0; i < left.Length; i++)
            Assert.IsTrue(JToken.DeepEquals(left[i], right[i]), $"{label} item mismatch at {i}");
    }
}
