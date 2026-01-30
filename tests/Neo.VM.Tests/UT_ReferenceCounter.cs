// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ReferenceCounter.cs file belongs to the neo project and is free
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
using System;
using System.Collections.Generic;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.Test;

[TestClass]
public class UT_ReferenceCounter
{
    [TestMethod]
    public void TestCircularReferences()
    {
        byte[] script = BuildCircularReferencesScript();
        using ExecutionEngine engine = new();
        Debugger debugger = new(engine);
        engine.LoadScript(script);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(1, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(2, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(2, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(3, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(4, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(3, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(4, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(5, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(5, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(4, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(5, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(4, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(5, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(6, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(6, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(7, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(6, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(7, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(7, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(8, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(7, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(8, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(7, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(8, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(7, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(8, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(9, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(6, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(5, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.HALT, debugger.Execute());
        Assert.AreEqual(4, engine.ReferenceCounter.Count);
    }

    [TestMethod]
    public void TestRemoveReferrer()
    {
        byte[] script = BuildRemoveReferrerScript();
        using ExecutionEngine engine = new();
        Debugger debugger = new(engine);
        engine.LoadScript(script);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(1, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(2, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(2, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(3, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(4, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(4, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(3, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(4, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(3, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.BREAK, debugger.StepInto());
        Assert.AreEqual(2, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.HALT, debugger.Execute());
        Assert.AreEqual(1, engine.ReferenceCounter.Count);
    }

    [TestMethod]
    public void TestCheckZeroReferredWithArray()
    {
        using ScriptBuilder sb = new();

        sb.EmitPush(ExecutionEngineLimits.Default.MaxStackSize - 1);
        sb.Emit(OpCode.NEWARRAY);

        // Good with MaxStackSize

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual((int)ExecutionEngineLimits.Default.MaxStackSize, engine.ReferenceCounter.Count);
        }

        // Fault with MaxStackSize+1

        sb.Emit(OpCode.PUSH1);

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.AreEqual((int)ExecutionEngineLimits.Default.MaxStackSize + 1, engine.ReferenceCounter.Count);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred()
    {
        using ScriptBuilder sb = new();

        for (int x = 0; x < ExecutionEngineLimits.Default.MaxStackSize; x++)
            sb.Emit(OpCode.PUSH1);

        // Good with MaxStackSize

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual((int)ExecutionEngineLimits.Default.MaxStackSize, engine.ReferenceCounter.Count);
        }

        // Fault with MaxStackSize+1

        sb.Emit(OpCode.PUSH1);

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.AreEqual((int)ExecutionEngineLimits.Default.MaxStackSize + 1, engine.ReferenceCounter.Count);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred_PopItemArray()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.POPITEM);

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            engine.Push(new Array(engine.ReferenceCounter, new StackItem[] { 42 }));
            Assert.AreEqual(2, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(1, engine.ResultStack.Count);

            Assert.AreEqual(1, engine.ReferenceCounter.Count);
            engine.ReferenceCounter.CheckZeroReferred();
            Assert.AreEqual(1, engine.ReferenceCounter.Count);


            engine.ResultStack.Pop(); // pop Array from stack.

            Assert.AreEqual(0, engine.ReferenceCounter.Count);
            engine.ReferenceCounter.CheckZeroReferred();
            Assert.AreEqual(0, engine.ReferenceCounter.Count);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred_Append()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.APPEND);

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            engine.Push(new Array(engine.ReferenceCounter, new StackItem[] { }));
            engine.Push(new Integer(42));
            Assert.AreEqual(2, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ResultStack.Count);
            Assert.AreEqual(1, engine.ReferenceCounter.Count);
            engine.ReferenceCounter.CheckZeroReferred();
            Assert.AreEqual(0, engine.ReferenceCounter.Count);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred_DupAppend()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.DUP);
        sb.Emit(OpCode.PUSH0);
        sb.Emit(OpCode.APPEND);

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            engine.Push(new Array(engine.ReferenceCounter, new StackItem[] { }));
            Assert.AreEqual(1, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(1, engine.ResultStack.Count);

            Assert.AreEqual(2, engine.ReferenceCounter.Count);
            engine.ReferenceCounter.CheckZeroReferred();
            Assert.AreEqual(2, engine.ReferenceCounter.Count);

            engine.ResultStack.Pop(); // pop Array from stack.

            Assert.AreEqual(1, engine.ReferenceCounter.Count);
            engine.ReferenceCounter.CheckZeroReferred();
            Assert.AreEqual(0, engine.ReferenceCounter.Count);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred_SetItemMap()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.SETITEM);

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            engine.Push(new Map(engine.ReferenceCounter));
            engine.Push(new Integer(0));
            engine.Push(new Integer(100500));
            Assert.AreEqual(3, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ResultStack.Count);

            Assert.AreEqual(2, engine.ReferenceCounter.Count);
            engine.ReferenceCounter.CheckZeroReferred();
            Assert.AreEqual(0, engine.ReferenceCounter.Count);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred_DupSetItemMap()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.DUP);
        sb.Emit(OpCode.PUSH0);
        sb.Emit(OpCode.PUSH1);
        sb.Emit(OpCode.SETITEM);

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            engine.Push(new Map(engine.ReferenceCounter));
            Assert.AreEqual(1, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(1, engine.ResultStack.Count);

            Assert.AreEqual(3, engine.ReferenceCounter.Count);
            engine.ReferenceCounter.CheckZeroReferred();
            Assert.AreEqual(3, engine.ReferenceCounter.Count);


            engine.ResultStack.Pop(); // pop Map from stack.

            Assert.AreEqual(2, engine.ReferenceCounter.Count);
            engine.ReferenceCounter.CheckZeroReferred();
            Assert.AreEqual(0, engine.ReferenceCounter.Count);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred_SetItemArray()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.SETITEM);

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            engine.Push(new Array(engine.ReferenceCounter, new StackItem[] { 42 }));
            engine.Push(new Integer(0));
            engine.Push(new Array(engine.ReferenceCounter, new StackItem[] { 42 }));
            Assert.AreEqual(5, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ResultStack.Count);

            Assert.AreEqual(2, engine.ReferenceCounter.Count);
            engine.ReferenceCounter.CheckZeroReferred();
            Assert.AreEqual(0, engine.ReferenceCounter.Count);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred_RemoveArray()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.REMOVE);

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            engine.Push(new Array(engine.ReferenceCounter, new StackItem[] { 42 }));
            engine.Push(new Integer(0));
            Assert.AreEqual(3, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ResultStack.Count);

            Assert.AreEqual(0, engine.ReferenceCounter.Count);
            engine.ReferenceCounter.CheckZeroReferred();
            Assert.AreEqual(0, engine.ReferenceCounter.Count);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred_RemoveStruct()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.REMOVE);

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            engine.Push(new Struct(engine.ReferenceCounter, new StackItem[] { 42 }));
            engine.Push(new Integer(0));
            Assert.AreEqual(3, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ResultStack.Count);

            Assert.AreEqual(0, engine.ReferenceCounter.Count);
            engine.ReferenceCounter.CheckZeroReferred();
            Assert.AreEqual(0, engine.ReferenceCounter.Count);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred_RemoveMap()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.REMOVE);

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            engine.Push(new Map(engine.ReferenceCounter) { [new Integer(0)] = StackItem.True });
            engine.Push(new Integer(0));
            Assert.AreEqual(4, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ResultStack.Count);

            Assert.AreEqual(0, engine.ReferenceCounter.Count);
            engine.ReferenceCounter.CheckZeroReferred();
            Assert.AreEqual(0, engine.ReferenceCounter.Count);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred_DupRemoveArray()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.DUP);
        sb.Emit(OpCode.PUSH0);
        sb.Emit(OpCode.REMOVE);

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            engine.Push(new Array(engine.ReferenceCounter, new StackItem[] { 42 }));
            Assert.AreEqual(2, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(1, engine.ResultStack.Count);

            Assert.AreEqual(1, engine.ReferenceCounter.Count);
            engine.ReferenceCounter.CheckZeroReferred();
            Assert.AreEqual(1, engine.ReferenceCounter.Count);
        }
    }


    [TestMethod]
    public void TestCheckZeroReferred_DupRemoveStruct()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.DUP);
        sb.Emit(OpCode.PUSH0);
        sb.Emit(OpCode.REMOVE);

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            engine.Push(new Struct(engine.ReferenceCounter, new StackItem[] { 42 }));
            Assert.AreEqual(2, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(1, engine.ResultStack.Count);

            Assert.AreEqual(1, engine.ReferenceCounter.Count);
            engine.ReferenceCounter.CheckZeroReferred();
            Assert.AreEqual(1, engine.ReferenceCounter.Count);
        }
    }


    [TestMethod]
    public void TestCheckZeroReferred_DupRemoveMap()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.DUP);
        sb.Emit(OpCode.PUSH0);
        sb.Emit(OpCode.REMOVE);

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            engine.Push(new Map(engine.ReferenceCounter) { [new Integer(0)] = StackItem.True });
            Assert.AreEqual(3, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(1, engine.ResultStack.Count);

            Assert.AreEqual(1, engine.ReferenceCounter.Count);
            engine.ReferenceCounter.CheckZeroReferred();
            Assert.AreEqual(1, engine.ReferenceCounter.Count);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred_ClearItemsArray()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.CLEARITEMS);

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            engine.Push(new Array(engine.ReferenceCounter, new StackItem[] { 42 }));
            Assert.AreEqual(2, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ResultStack.Count);

            Assert.AreEqual(0, engine.ReferenceCounter.Count);
            engine.ReferenceCounter.CheckZeroReferred();
            Assert.AreEqual(0, engine.ReferenceCounter.Count);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred_ClearItemsStruct()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.CLEARITEMS);

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            engine.Push(new Struct(engine.ReferenceCounter, new StackItem[] { 42 }));
            Assert.AreEqual(2, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ResultStack.Count);

            Assert.AreEqual(0, engine.ReferenceCounter.Count);
            engine.ReferenceCounter.CheckZeroReferred();
            Assert.AreEqual(0, engine.ReferenceCounter.Count);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred_ClearItemsMap()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.CLEARITEMS);

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            engine.Push(new Map(engine.ReferenceCounter) { [new Integer(0)] = StackItem.True });
            Assert.AreEqual(3, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ResultStack.Count);

            Assert.AreEqual(0, engine.ReferenceCounter.Count);
            engine.ReferenceCounter.CheckZeroReferred();
            Assert.AreEqual(0, engine.ReferenceCounter.Count);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred_DupClearItemsArray()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.DUP);
        sb.Emit(OpCode.CLEARITEMS);

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            engine.Push(new Array(engine.ReferenceCounter, new StackItem[] { 42 }));
            Assert.AreEqual(2, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(1, engine.ResultStack.Count);

            Assert.AreEqual(1, engine.ReferenceCounter.Count);
            engine.ReferenceCounter.CheckZeroReferred();
            Assert.AreEqual(1, engine.ReferenceCounter.Count);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred_DupClearItemsStruct()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.DUP);
        sb.Emit(OpCode.CLEARITEMS);

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            engine.Push(new Struct(engine.ReferenceCounter, new StackItem[] { 42 }));
            Assert.AreEqual(2, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(1, engine.ResultStack.Count);

            Assert.AreEqual(1, engine.ReferenceCounter.Count);
            engine.ReferenceCounter.CheckZeroReferred();
            Assert.AreEqual(1, engine.ReferenceCounter.Count);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred_DupClearItemsMap()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.DUP);
        sb.Emit(OpCode.CLEARITEMS);

        using (ExecutionEngine engine = new())
        {
            engine.LoadScript(sb.ToArray());
            Assert.AreEqual(0, engine.ReferenceCounter.Count);

            engine.Push(new Map(engine.ReferenceCounter) { [new Integer(0)] = StackItem.True });
            Assert.AreEqual(3, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(1, engine.ResultStack.Count);

            Assert.AreEqual(1, engine.ReferenceCounter.Count);
            engine.ReferenceCounter.CheckZeroReferred();
            Assert.AreEqual(1, engine.ReferenceCounter.Count);
        }
    }

    [TestMethod]
    public void TestArrayNoPush()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.RET);
        using ExecutionEngine engine = new();
        engine.LoadScript(sb.ToArray());
        Assert.AreEqual(0, engine.ReferenceCounter.Count);
        Array array = new(engine.ReferenceCounter, new StackItem[] { 1, 2, 3, 4 });
        Assert.AreEqual(array.Count, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.HALT, engine.Execute());
        Assert.AreEqual(array.Count, engine.ReferenceCounter.Count);
    }

    [TestMethod]
    public void TestInvalidReferenceStackItem()
    {
        var reference = new ReferenceCounter();
        var arr = new Array(reference);
        var arr2 = new Array();

        for (var i = 0; i < 10; i++)
        {
            arr2.Add(i);
        }

        Assert.ThrowsExactly<InvalidOperationException>(() => arr.Add(arr2));
    }

    [TestMethod]
    public void TestMarkSweepMatchesReferenceCounterOnCircularScript()
    {
        var script = BuildCircularReferencesScript();
        AssertReferenceCountersProduceSameResult(script, 4);
    }

    [TestMethod]
    public void TestMarkSweepMatchesReferenceCounterOnRemoveReferrerScript()
    {
        var script = BuildRemoveReferrerScript();
        AssertReferenceCountersProduceSameResult(script, 1);
    }

    private static void AssertReferenceCountersProduceSameResult(byte[] script, int expectedCount)
    {
        using (CounterEngine engine = new(new ReferenceCounter()))
        {
            engine.LoadScript(script);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(expectedCount, engine.ReferenceCounter.Count);
        }

        using CounterEngine markSweepEngine = new(new MarkSweepReferenceCounter());
        markSweepEngine.LoadScript(script);
        Assert.AreEqual(VMState.HALT, markSweepEngine.Execute());
        Assert.AreEqual(expectedCount, markSweepEngine.ReferenceCounter.Count);
    }

    private static byte[] BuildCircularReferencesScript()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.INITSSLOT, new byte[] { 1 });
        sb.EmitPush(0);
        sb.Emit(OpCode.NEWARRAY);
        sb.Emit(OpCode.DUP);
        sb.Emit(OpCode.DUP);
        sb.Emit(OpCode.APPEND);
        sb.Emit(OpCode.DUP);
        sb.EmitPush(0);
        sb.Emit(OpCode.NEWARRAY);
        sb.Emit(OpCode.STSFLD0);
        sb.Emit(OpCode.LDSFLD0);
        sb.Emit(OpCode.APPEND);
        sb.Emit(OpCode.LDSFLD0);
        sb.EmitPush(0);
        sb.Emit(OpCode.NEWARRAY);
        sb.Emit(OpCode.TUCK);
        sb.Emit(OpCode.APPEND);
        sb.EmitPush(0);
        sb.Emit(OpCode.NEWARRAY);
        sb.Emit(OpCode.TUCK);
        sb.Emit(OpCode.APPEND);
        sb.Emit(OpCode.LDSFLD0);
        sb.Emit(OpCode.APPEND);
        sb.Emit(OpCode.PUSHNULL);
        sb.Emit(OpCode.STSFLD0);
        sb.Emit(OpCode.DUP);
        sb.EmitPush(1);
        sb.Emit(OpCode.REMOVE);
        sb.Emit(OpCode.STSFLD0);
        sb.Emit(OpCode.RET);
        return sb.ToArray();
    }

    private static byte[] BuildRemoveReferrerScript()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.INITSSLOT, new byte[] { 1 });
        sb.EmitPush(0);
        sb.Emit(OpCode.NEWARRAY);
        sb.Emit(OpCode.DUP);
        sb.EmitPush(0);
        sb.Emit(OpCode.NEWARRAY);
        sb.Emit(OpCode.STSFLD0);
        sb.Emit(OpCode.LDSFLD0);
        sb.Emit(OpCode.APPEND);
        sb.Emit(OpCode.DROP);
        sb.Emit(OpCode.RET);
        return sb.ToArray();
    }

    private sealed class CounterEngine : ExecutionEngine
    {
        public CounterEngine(IReferenceCounter counter)
            : base(null, counter, ExecutionEngineLimits.Default)
        {
        }
    }
}
