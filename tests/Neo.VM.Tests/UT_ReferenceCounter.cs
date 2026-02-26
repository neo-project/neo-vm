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
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.INITSSLOT, new byte[] { 1 }); //{}|{null}:1
        sb.EmitPush(0); //{0}|{null}:2
        sb.Emit(OpCode.NEWARRAY); //{A[]}|{null}:2
        sb.Emit(OpCode.DUP); //{A[],A[]}|{null}:3
        sb.Emit(OpCode.DUP); //{A[],A[],A[]}|{null}:4
        sb.Emit(OpCode.APPEND); //{A[A]}|{null}:3
        sb.Emit(OpCode.DUP); //{A[A],A[A]}|{null}:4
        sb.EmitPush(0); //{A[A],A[A],0}|{null}:5
        sb.Emit(OpCode.NEWARRAY); //{A[A],A[A],B[]}|{null}:5
        sb.Emit(OpCode.STSFLD0); //{A[A],A[A]}|{B[]}:4
        sb.Emit(OpCode.LDSFLD0); //{A[A],A[A],B[]}|{B[]}:5
        sb.Emit(OpCode.APPEND); //{A[A,B]}|{B[]}:4
        sb.Emit(OpCode.LDSFLD0); //{A[A,B],B[]}|{B[]}:5
        sb.EmitPush(0); //{A[A,B],B[],0}|{B[]}:6
        sb.Emit(OpCode.NEWARRAY); //{A[A,B],B[],C[]}|{B[]}:6
        sb.Emit(OpCode.TUCK); //{A[A,B],C[],B[],C[]}|{B[]}:7
        sb.Emit(OpCode.APPEND); //{A[A,B],C[]}|{B[C]}:6
        sb.EmitPush(0); //{A[A,B],C[],0}|{B[C]}:7
        sb.Emit(OpCode.NEWARRAY); //{A[A,B],C[],D[]}|{B[C]}:7
        sb.Emit(OpCode.TUCK); //{A[A,B],D[],C[],D[]}|{B[C]}:8
        sb.Emit(OpCode.APPEND); //{A[A,B],D[]}|{B[C[D]]}:7
        sb.Emit(OpCode.LDSFLD0); //{A[A,B],D[],B[C]}|{B[C[D]]}:8
        sb.Emit(OpCode.APPEND); //{A[A,B]}|{B[C[D[B]]]}:7
        sb.Emit(OpCode.PUSHNULL); //{A[A,B],null}|{B[C[D[B]]]}:8
        sb.Emit(OpCode.STSFLD0); //{A[A,B[C[D[B]]]]}|{null}:7
        sb.Emit(OpCode.DUP); //{A[A,B[C[D[B]]]],A[A,B]}|{null}:8
        sb.EmitPush(1); //{A[A,B[C[D[B]]]],A[A,B],1}|{null}:9
        sb.Emit(OpCode.REMOVE); //{A[A]}|{null}:3
        sb.Emit(OpCode.STSFLD0); //{}|{A[A]}:2
        sb.Emit(OpCode.RET); //{}:0

        using ExecutionEngine engine = new();
        Debugger debugger = new(engine);
        engine.LoadScript(sb.ToArray());
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
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.INITSSLOT, new byte[] { 1 }); //{}|{null}:1
        sb.EmitPush(0); //{0}|{null}:2
        sb.Emit(OpCode.NEWARRAY); //{A[]}|{null}:2
        sb.Emit(OpCode.DUP); //{A[],A[]}|{null}:3
        sb.EmitPush(0); //{A[],A[],0}|{null}:4
        sb.Emit(OpCode.NEWARRAY); //{A[],A[],B[]}|{null}:4
        sb.Emit(OpCode.STSFLD0); //{A[],A[]}|{B[]}:3
        sb.Emit(OpCode.LDSFLD0); //{A[],A[],B[]}|{B[]}:4
        sb.Emit(OpCode.APPEND); //{A[B]}|{B[]}:3
        sb.Emit(OpCode.DROP); //{}|{B[]}:1
        sb.Emit(OpCode.RET); //{}:0

        using ExecutionEngine engine = new();
        Debugger debugger = new(engine);
        engine.LoadScript(sb.ToArray());
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
}
