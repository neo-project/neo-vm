// Copyright (C) 2015-2026 The Neo Project.
//
// VMJsonTestBase.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Test.Extensions;
using Neo.Test.Types;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using Array = Neo.VM.Types.Array;
using Boolean = Neo.VM.Types.Boolean;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.Test;

public abstract class VMJsonTestBase
{
    /// <summary>
    /// Execute this test
    /// </summary>
    /// <param name="ut">Test</param>
    public static void ExecuteTest(VMUT ut)
    {
        foreach (var test in ut.Tests)
        {
            Assert.IsFalse(string.IsNullOrEmpty(test.Name), "Name is required");

            var refCounter = new ReferenceCounter();
            using TestEngine engine = new TestEngine(refCounter, ExecutionEngineLimits.Default);
            Debugger debugger = new(engine);

            if (test.Script.Length > 0)
            {
                engine.LoadScript(test.Script);
            }

            // Execute Steps
            if (test.Steps != null)
            {
                foreach (var step in test.Steps)
                {
                    // Actions
                    if (step.Actions != null) foreach (var run in step.Actions)
                    {
                        switch (run)
                        {
                            case VMUTActionType.Execute: debugger.Execute(); break;
                            case VMUTActionType.StepInto: debugger.StepInto(); break;
                            case VMUTActionType.StepOut: debugger.StepOut(); break;
                            case VMUTActionType.StepOver: debugger.StepOver(); break;
                        }
                    }

                    // Review results
                    var add = string.IsNullOrEmpty(step.Name) ? "" : "-" + step.Name;

                    AssertResult(step.Result, engine, $"{ut.Category}-{ut.Name}-{test.Name}{add}: ");
                }
            }
        }
    }

    /// <summary>
    /// Assert result
    /// </summary>
    /// <param name="actual">Actual engine</param>
    /// <param name="expected">Expected engine state</param>
    /// <param name="message">Message</param>
    private static void AssertResult(VMUTExecutionEngineState expected, TestEngine actual, string message)
    {
        AssertAreEqual(expected.State.ToString().ToLowerInvariant(), actual.State.ToString().ToLowerInvariant(), message + $" Engine state mismatch. Actual FaultException: {actual.FaultException?.ToString()}");
        if (actual.State == VMState.FAULT)
        {
            if (expected.ExceptionMessage != null)
            {
                AssertAreEqual(expected.ExceptionMessage, actual.FaultException.Message, message + " [Exception]");
            }
            return;
        }
        AssertResult(expected.InvocationStack, actual.InvocationStack, message + " [Invocation stack]");
        if (expected.ResultStack is not null) // compare stack only if requested (for circular references it's impossible to represent stack in JSON).
            AssertResult(expected.ResultStack, actual.ResultStack, message + " [Result stack]");
        if (expected.Refs != null)
            AssertAreEqual(expected.Refs, actual.ReferenceCounter.Count, message + " Reference counter value mismatch");
        if (expected.RunStats != null)
            AssertResult(expected.RunStats, actual.LastRunStats, message + " [RunStats]");
    }

    /// <summary>
    /// Assert RunStats of the last executed instruction. Only the fields present in
    /// <paramref name="expected"/> are checked.
    /// </summary>
    /// <param name="expected">Expected RunStats (only non-null fields are checked)</param>
    /// <param name="actual">Actual RunStats collected by the last executed instruction</param>
    /// <param name="message">Message</param>
    private static void AssertResult(VMUTRunStats expected, RunStats actual, string message)
    {
        if (expected.Type != null)
            AssertAreEqual(expected.Type.Value, actual.Type, message + " Type mismatch");
        if (expected.Length != null)
            AssertAreEqual(expected.Length, actual.Length, message + " Length mismatch");
        if (expected.RefsDelta != null)
            AssertAreEqual(expected.RefsDelta, actual.RefsDelta, message + " RefsDelta mismatch");
        if (expected.NClonedItems != null)
            AssertAreEqual(expected.NClonedItems, actual.NClonedItems, message + " NClonedItems mismatch");
    }

    /// <summary>
    /// Assert invocation stack
    /// </summary>
    /// <param name="actual">Actual invocation stack</param>
    /// <param name="expected">Expected execution context state</param>
    /// <param name="message">Message</param>
    private static void AssertResult(VMUTExecutionContextState[] expected, Stack<ExecutionContext> actual, string message)
    {
        AssertAreEqual(expected == null ? 0 : expected.Length, actual.Count, message + " Stack length mismatch");

        int x = 0;
        foreach (var context in actual)
        {
            var opcode = context.InstructionPointer >= context.Script.Length ? OpCode.RET : context.Script[context.InstructionPointer];

            AssertAreEqual(expected[x].NextInstruction, opcode, message + " NextInstruction mismatch");
            AssertAreEqual(expected[x].InstructionPointer, context.InstructionPointer, message + " InstructionPointer mismatch");

            // Check stack
            if (expected[x].EvaluationStack is not null)
                AssertResult(expected[x].EvaluationStack, context.EvaluationStack, message + " [EvaluationStack]");

            // Check slots
            AssertResult(expected[x].Arguments, context.Arguments, message + " [Arguments]");
            AssertResult(expected[x].LocalVariables, context.LocalVariables, message + " [LocalVariables]");
            AssertResult(expected[x].StaticFields, context.StaticFields, message + " [StaticFields]");

            x++;
        }
    }

    /// <summary>
    /// Assert result stack
    /// </summary>
    /// <param name="actual">Actual evaluation stack</param>
    /// <param name="expected">Expected stack content</param>
    /// <param name="message">Message</param>
    private static void AssertResult(VMUTStackItem[] expected, EvaluationStack actual, string message)
    {
        AssertAreEqual(expected == null ? 0 : expected.Length, actual.Count, message + " Stack length mismatch");

        for (int x = 0, max = actual.Count; x < max; x++)
        {
            AssertAreEqual(PrepareJsonItem(expected[x]).ToJsonString(), ItemToJson(actual.Peek(x)).ToJsonString(), message + $" Stack item #{x} mismatch");
        }
    }

    /// <summary>
    /// Assert result slot
    /// </summary>
    /// <param name="actual">Actual slot</param>
    /// <param name="expected">Expected slot content</param>
    /// <param name="message">Message</param>
    private static void AssertResult(VMUTStackItem[] expected, Slot actual, string message)
    {
        AssertAreEqual(expected == null ? 0 : expected.Length, actual == null ? 0 : actual.Count, message + " Slot length mismatch");

        for (int x = 0, max = actual == null ? 0 : actual.Count; x < max; x++)
        {
            AssertAreEqual(PrepareJsonItem(expected[x]).ToJsonString(), ItemToJson(actual[x]).ToJsonString(), message + $" Stack item #{x} mismatch");
        }
    }

    private static string AsString(JsonNode node)
    {
        if (node is null) return null;
        return node is JsonValue v && v.TryGetValue<string>(out var s) ? s : node.ToJsonString();
    }

    private static JsonNode FromBytes(byte[] data) => JsonValue.Create(Convert.ToBase64String(data));

    private static JsonObject PrepareJsonItem(VMUTStackItem item)
    {
        var ret = new JsonObject
        {
            ["type"] = item.Type.ToString()
        };

        switch (item.Type)
        {
            case VMUTStackItemType.Null:
                {
                    break;
                }
            case VMUTStackItemType.Pointer:
                {
                    ret["value"] = item.Value.GetValue<int>();
                    break;
                }
            case VMUTStackItemType.String:
                {
                    // Easy access
                    ret["type"] = VMUTStackItemType.ByteString.ToString();
                    ret["value"] = FromBytes(Encoding.UTF8.GetBytes(AsString(item.Value)));
                    break;
                }
            case VMUTStackItemType.ByteString:
            case VMUTStackItemType.Buffer:
                {
                    var value = AsString(item.Value);
                    Assert.IsTrue(string.IsNullOrEmpty(value) || value.StartsWith("0x"), $"'0x' prefix required for value: '{value}'");
                    ret["value"] = FromBytes(value.FromHexString());
                    break;
                }
            case VMUTStackItemType.Integer:
                {
                    // Ensure format
                    ret["value"] = AsString(item.Value);
                    break;
                }
            case VMUTStackItemType.Struct:
            case VMUTStackItemType.Array:
                {
                    var array = new JsonArray();

                    foreach (var entry in item.Value.AsArray())
                    {
                        array.Add(PrepareJsonItem(entry.ToJsonString().DeserializeJson<VMUTStackItem>()));
                    }

                    ret["value"] = array;
                    break;
                }
            case VMUTStackItemType.Map:
                {
                    var obj = new JsonObject();

                    foreach (var prop in item.Value.AsObject())
                    {
                        obj[prop.Key] = PrepareJsonItem(prop.Value.ToJsonString().DeserializeJson<VMUTStackItem>());
                    }

                    ret["value"] = obj;
                    break;
                }
            default:
                {
                    ret["value"] = item.Value?.DeepClone();
                    break;
                }
        }

        return ret;
    }

    private static JsonObject ItemToJson(StackItem item)
    {
        if (item == null) return null;

        JsonNode value;
        string type = item.GetType().Name;

        switch (item)
        {
            case Null _:
                {
                    return new JsonObject
                    {
                        ["type"] = type,
                    };
                }
            case Pointer p:
                {
                    return new JsonObject
                    {
                        ["type"] = type,
                        ["value"] = p.Position
                    };
                }
            case Boolean v: value = JsonValue.Create(v.GetBoolean()); break;
            case Integer v: value = JsonValue.Create(v.GetInteger().ToString()); break;
            case ByteString v: value = FromBytes(v.GetSpan().ToArray()); break;
            case Buffer v: value = FromBytes(v.InnerBuffer.ToArray()); break;
            //case VM.Types.Struct v:
            case Array v:
                {
                    var jarray = new JsonArray();

                    foreach (var entry in v)
                    {
                        jarray.Add(ItemToJson(entry));
                    }

                    value = jarray;
                    break;
                }
            case Map v:
                {
                    var jdic = new JsonObject();

                    foreach (var entry in v)
                    {
                        jdic.Add(entry.Key.GetSpan().ToArray().ToHexString(), ItemToJson(entry.Value));
                    }

                    value = jdic;
                    break;
                }
            case InteropInterface v:
                {
                    type = "Interop";
                    var obj = v.GetInterface<object>();

                    value = JsonValue.Create(obj.GetType().Name);
                    break;
                }
            default: throw new NotImplementedException();
        }

        return new JsonObject
        {
            ["type"] = type,
            ["value"] = value
        };
    }

    /// <summary>
    /// Assert with message
    /// </summary>
    /// <param name="expected">A</param>
    /// <param name="actual">B</param>
    /// <param name="message">Message</param>
    private static void AssertAreEqual(object expected, object actual, string message)
    {
        if (expected is byte[] ba) expected = ba.ToHexString().ToUpperInvariant();
        if (actual is byte[] bb) actual = bb.ToHexString().ToUpperInvariant();

        if (expected.ToJson() != actual.ToJson())
        {
            throw new Exception(message +
                $"{Environment.NewLine}Expected:{Environment.NewLine + expected.ToString() + Environment.NewLine}Actual:{Environment.NewLine + actual.ToString()}");
        }
    }
}
