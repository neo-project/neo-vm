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
using Neo.Test.Types;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Numerics;
using Array = Neo.VM.Types.Array;
using Buffer = Neo.VM.Types.Buffer;

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
        Assert.AreEqual(1, engine.ReferenceCounter.Count);
        Assert.AreEqual(VMState.HALT, debugger.Execute());
        Assert.AreEqual(0, engine.ReferenceCounter.Count);
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

            engine.Push(new Array(new StackItem[] { 42 }));
            Assert.AreEqual(2, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(1, engine.ResultStack.Count);

            Assert.AreEqual(1, engine.ReferenceCounter.Count);

            engine.ResultStack.Pop(); // pop Array from stack.

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

            engine.Push(new Array(new StackItem[] { }));
            engine.Push(new Integer(42));
            Assert.AreEqual(2, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ResultStack.Count);
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

            engine.Push(new Array(new StackItem[] { }));
            Assert.AreEqual(1, engine.ReferenceCounter.Count);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(1, engine.ResultStack.Count);

            Assert.AreEqual(2, engine.ReferenceCounter.Count);

            engine.ResultStack.Pop(); // pop Array from stack.

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

            engine.Push(new Map());
            engine.Push(new Integer(0));
            engine.Push(new Integer(100500));
            Assert.AreEqual(3, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ResultStack.Count);

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

            engine.Push(new Map());
            Assert.AreEqual(1, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(1, engine.ResultStack.Count);

            Assert.AreEqual(3, engine.ReferenceCounter.Count);

            engine.ResultStack.Pop(); // pop Map from stack.

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

            engine.Push(new Array(new StackItem[] { 42 }));
            engine.Push(new Integer(0));
            engine.Push(new Array(new StackItem[] { 42 }));
            Assert.AreEqual(5, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ResultStack.Count);

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

            engine.Push(new Array(new StackItem[] { 42 }));
            engine.Push(new Integer(0));
            Assert.AreEqual(3, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ResultStack.Count);

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

            engine.Push(new Struct(new StackItem[] { 42 }));
            engine.Push(new Integer(0));
            Assert.AreEqual(3, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ResultStack.Count);

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

            engine.Push(new Map() { [new Integer(0)] = StackItem.True });
            engine.Push(new Integer(0));
            Assert.AreEqual(4, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ResultStack.Count);

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

            engine.Push(new Array(new StackItem[] { 42 }));
            Assert.AreEqual(2, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(1, engine.ResultStack.Count);

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

            engine.Push(new Struct(new StackItem[] { 42 }));
            Assert.AreEqual(2, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(1, engine.ResultStack.Count);

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

            engine.Push(new Map() { [new Integer(0)] = StackItem.True });
            Assert.AreEqual(3, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(1, engine.ResultStack.Count);

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

            engine.Push(new Array(new StackItem[] { 42 }));
            Assert.AreEqual(2, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ResultStack.Count);

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

            engine.Push(new Struct(new StackItem[] { 42 }));
            Assert.AreEqual(2, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ResultStack.Count);

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

            engine.Push(new Map() { [new Integer(0)] = StackItem.True });
            Assert.AreEqual(3, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ResultStack.Count);

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

            engine.Push(new Array(new StackItem[] { 42 }));
            Assert.AreEqual(2, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(1, engine.ResultStack.Count);

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

            engine.Push(new Struct(new StackItem[] { 42 }));
            Assert.AreEqual(2, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(1, engine.ResultStack.Count);

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

            engine.Push(new Map() { [new Integer(0)] = StackItem.True });
            Assert.AreEqual(3, engine.ReferenceCounter.Count);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(1, engine.ResultStack.Count);

            Assert.AreEqual(1, engine.ReferenceCounter.Count);
        }
    }

    [TestMethod]
    public void TestArrayNoPush()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.RET);
        using ExecutionEngine engine = new(null, new ReferenceCounter(), ExecutionEngineLimits.Default);
        engine.LoadScript(sb.ToArray());
        Assert.AreEqual(0, engine.ReferenceCounter.Count);
        Array array = new(new StackItem[] { 1, 2, 3, 4 });
        Assert.AreEqual(0, engine.ReferenceCounter.Count); // Array is not pushed to stack.
        Assert.AreEqual(VMState.HALT, engine.Execute());
        Assert.AreEqual(0, engine.ReferenceCounter.Count);
    }

    [TestMethod]
    public void TestPostExecuteInstruction()
    {
        var refCounter = new ReferenceCounter();
        for (int i = 0; i < ExecutionEngineLimits.Default.MaxStackSize; i++)
        {
            refCounter.AddStackReference(StackItem.Null);
        }
        refCounter.PostExecuteInstruction();
        refCounter.AddStackReference(StackItem.Null);
        Assert.ThrowsExactly<InvalidOperationException>(() => refCounter.PostExecuteInstruction());
    }

    [TestMethod]
    public void TestAdd_MatchesNeoGo()
    {
        var rc = new ReferenceCounter();
        Assert.AreEqual(0, rc.Count);

        rc.AddStackReference(StackItem.Null);
        Assert.AreEqual(1, rc.Count);

        rc.AddStackReference(StackItem.Null);
        Assert.AreEqual(2, rc.Count); // count scalar items twice

        var arr = new Array(new StackItem[] { new ByteString(new byte[] { 1 }), StackItem.False });
        rc.AddStackReference(arr);
        Assert.AreEqual(5, rc.Count); // array + 2 elements

        rc.AddStackReference(arr);
        Assert.AreEqual(6, rc.Count); // count only array

        rc.RemoveStackReference(arr);
        Assert.AreEqual(5, rc.Count);

        rc.RemoveStackReference(arr);
        Assert.AreEqual(2, rc.Count);

        var map = new Map { [new ByteString("some"u8.ToArray())] = StackItem.False };
        rc.AddStackReference(map);
        Assert.AreEqual(5, rc.Count); // map + key + value

        rc.AddStackReference(map);
        Assert.AreEqual(6, rc.Count); // map only

        rc.RemoveStackReference(map);
        Assert.AreEqual(5, rc.Count);

        rc.RemoveStackReference(map);
        Assert.AreEqual(2, rc.Count);
    }

    [TestMethod]
    public void TestCheckZeroReferred_DupPopItem()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.DUP);
        sb.Emit(OpCode.POPITEM);

        using ExecutionEngine engine = new();
        engine.LoadScript(sb.ToArray());
        engine.Push(new Array(new StackItem[] { 42, 42 }));
        Assert.AreEqual(3, engine.ReferenceCounter.Count);

        Assert.AreEqual(VMState.HALT, engine.Execute());
        Assert.AreEqual(2, engine.ResultStack.Count);
        Assert.AreEqual(3, engine.ReferenceCounter.Count);

        engine.ResultStack.Pop();
        Assert.AreEqual(1, engine.ResultStack.Count);
        Assert.AreEqual(2, engine.ReferenceCounter.Count);

        engine.ResultStack.Pop();
        Assert.AreEqual(0, engine.ResultStack.Count);
        Assert.AreEqual(0, engine.ReferenceCounter.Count);
    }

    [TestMethod]
    public void TestCheckZeroReferred_Unpack()
    {
        foreach (var (name, factory) in UnpackContainers())
        {
            using ScriptBuilder sb = new();
            sb.Emit(OpCode.UNPACK);

            using ExecutionEngine engine = new();
            engine.LoadScript(sb.ToArray());
            engine.Push(factory(out int keyCount));
            Assert.AreEqual(3 + keyCount, engine.ReferenceCounter.Count, name);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(2 + keyCount, engine.ResultStack.Count, name);
            Assert.AreEqual((2 + keyCount) + 1, engine.ReferenceCounter.Count, name);

            engine.ResultStack.Pop();
            Assert.AreEqual(2 + keyCount, engine.ReferenceCounter.Count, name);

            engine.ResultStack.Pop();
            if (name == "map")
            {
                Assert.AreEqual(2, engine.ReferenceCounter.Count, name);
                engine.ResultStack.Pop();
            }
            Assert.AreEqual(0, engine.ResultStack.Count, name);
            Assert.AreEqual(0, engine.ReferenceCounter.Count, name);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred_DupUnpack()
    {
        foreach (var (name, factory) in UnpackContainers())
        {
            using ScriptBuilder sb = new();
            sb.Emit(OpCode.DUP);
            sb.Emit(OpCode.UNPACK);

            using ExecutionEngine engine = new();
            engine.LoadScript(sb.ToArray());
            engine.Push(factory(out int keyCount));
            Assert.AreEqual((2 + keyCount) + 1, engine.ReferenceCounter.Count, name);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual((2 + keyCount) + 1, engine.ResultStack.Count, name);
            Assert.AreEqual(3 + 1 + 2 * keyCount + 1, engine.ReferenceCounter.Count, name);

            engine.ResultStack.Pop();
            Assert.AreEqual(3 + 1 + 2 * keyCount, engine.ReferenceCounter.Count, name);

            engine.ResultStack.Pop();
            if (name == "map")
            {
                Assert.AreEqual(3 + 1 + keyCount, engine.ReferenceCounter.Count, name);
                engine.ResultStack.Pop();
            }
            Assert.AreEqual(1, engine.ResultStack.Count, name);
            Assert.AreEqual(3 + keyCount, engine.ReferenceCounter.Count, name);

            engine.ResultStack.Pop();
            Assert.AreEqual(0, engine.ResultStack.Count, name);
            Assert.AreEqual(0, engine.ReferenceCounter.Count, name);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred_Values()
    {
        foreach (var (name, factory) in ValuesContainers())
        {
            using ScriptBuilder sb = new();
            sb.Emit(OpCode.VALUES);

            using ExecutionEngine engine = new();
            engine.LoadScript(sb.ToArray());
            engine.Push(factory(out int keyCount));
            Assert.AreEqual(3 + keyCount, engine.ReferenceCounter.Count, name);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(1, engine.ResultStack.Count, name);
            Assert.AreEqual(3, engine.ReferenceCounter.Count, name);

            engine.ResultStack.Pop();
            Assert.AreEqual(0, engine.ResultStack.Count, name);
            Assert.AreEqual(0, engine.ReferenceCounter.Count, name);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred_DupValues()
    {
        foreach (var (name, factory) in ValuesContainers())
        {
            using ScriptBuilder sb = new();
            sb.Emit(OpCode.DUP);
            sb.Emit(OpCode.VALUES);

            using ExecutionEngine engine = new();
            engine.LoadScript(sb.ToArray());
            engine.Push(factory(out int keyCount));
            Assert.AreEqual(3 + keyCount, engine.ReferenceCounter.Count, name);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(2, engine.ResultStack.Count, name);
            Assert.AreEqual((3 + keyCount) + 3, engine.ReferenceCounter.Count, name);

            engine.ResultStack.Pop();
            Assert.AreEqual(3 + keyCount, engine.ReferenceCounter.Count, name);

            engine.ResultStack.Pop();
            Assert.AreEqual(0, engine.ResultStack.Count, name);
            Assert.AreEqual(0, engine.ReferenceCounter.Count, name);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred_SetItemCloneStruct()
    {
        foreach (var (name, factory) in SetItemCloneContainers())
        {
            using ScriptBuilder sb = new();
            sb.Emit(OpCode.DUP);
            sb.Emit(OpCode.PUSH0);
            sb.Emit(OpCode.PUSH3);
            sb.Emit(OpCode.PICK);
            sb.Emit(OpCode.SETITEM);

            using ExecutionEngine engine = new();
            engine.LoadScript(sb.ToArray());
            engine.Push(new Struct(new StackItem[] { 42 }));
            Assert.AreEqual(2, engine.ReferenceCounter.Count, name);
            engine.Push(factory(out int keyCount));
            Assert.AreEqual(2 + (2 + keyCount), engine.ReferenceCounter.Count, name);

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(2, engine.ResultStack.Count, name);
            Assert.AreEqual(2 + (3 + keyCount), engine.ReferenceCounter.Count, name);

            engine.ResultStack.Pop();
            Assert.AreEqual(2, engine.ReferenceCounter.Count, name);

            engine.ResultStack.Pop();
            Assert.AreEqual(0, engine.ResultStack.Count, name);
            Assert.AreEqual(0, engine.ReferenceCounter.Count, name);
        }
    }

    [TestMethod]
    public void TestCheckZeroReferred_PackMapSameKey_DifferentValues()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.PACKMAP);

        using ExecutionEngine engine = new();
        engine.LoadScript(sb.ToArray());
        engine.Push(StackItem.Null);
        engine.Push(new Integer(0));
        engine.Push(new Array(new StackItem[] { 42 }));
        engine.Push(new Integer(0));
        engine.Push(new Integer(2));
        Assert.AreEqual(5, engine.CurrentContext!.EvaluationStack.Count);
        Assert.AreEqual((1 + 1) + (2 + 1) + 1, engine.ReferenceCounter.Count);

        Assert.AreEqual(VMState.HALT, engine.Execute());
        Assert.AreEqual(1, engine.ResultStack.Count);
        Assert.AreEqual(1 + (1 + 1), engine.ReferenceCounter.Count);

        engine.ResultStack.Pop();
        Assert.AreEqual(0, engine.ResultStack.Count);
        Assert.AreEqual(0, engine.ReferenceCounter.Count);
    }

    [TestMethod]
    public void TestCheckZeroReferred_PackMapSameKey_SameValues()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.PACKMAP);

        using ExecutionEngine engine = new();
        engine.LoadScript(sb.ToArray());
        var arr = new Array(new StackItem[] { 42 });
        engine.Push(arr);
        engine.Push(new Integer(0));
        engine.Push(arr);
        engine.Push(new Integer(0));
        engine.Push(new Integer(2));
        Assert.AreEqual(5, engine.CurrentContext!.EvaluationStack.Count);
        Assert.AreEqual((2 + 1) + (1 + 1) + 1, engine.ReferenceCounter.Count);

        Assert.AreEqual(VMState.HALT, engine.Execute());
        Assert.AreEqual(1, engine.ResultStack.Count);
        Assert.AreEqual(1 + (1 + 2), engine.ReferenceCounter.Count);

        engine.ResultStack.Pop();
        Assert.AreEqual(0, engine.ResultStack.Count);
        Assert.AreEqual(0, engine.ReferenceCounter.Count);
    }

    [TestMethod]
    public void TestCheckZeroReferred_SetItemException()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.TRY, new byte[] { 4, 0 });
        sb.Emit(OpCode.SETITEM);

        using ExecutionEngine engine = new();
        engine.LoadScript(sb.ToArray());
        engine.Push(new Buffer(0));
        engine.Push(new Integer(0));
        engine.Push(new Array(new StackItem[] { 42 }));
        Assert.AreEqual(3, engine.CurrentContext!.EvaluationStack.Count);
        Assert.AreEqual(1 + 1 + 2, engine.ReferenceCounter.Count);

        Assert.AreEqual(VMState.HALT, engine.Execute());
        Assert.AreEqual(1, engine.ResultStack.Count);
        Assert.AreEqual(1, engine.ReferenceCounter.Count);
    }

    [TestMethod]
    public void TestNegativeRC_CircularClearItemsCannotUnderflow()
    {
        // Port of neo-go TestNegativeRC / nspcc-dev/neo-go#4312 and neo-vm#580:
        // CLEARITEMS on a self-referential array must not drive RC negative,
        // otherwise MaxStackSize can be bypassed.
        byte[] prog =
        [
            (byte)OpCode.INITSLOT, 0x01, 0x00,
            (byte)OpCode.PUSH16, (byte)OpCode.INC, (byte)OpCode.STLOC0,
            (byte)OpCode.NEWARRAY0,
            (byte)OpCode.DUP,
            (byte)OpCode.DUP,
            (byte)OpCode.APPEND,
            (byte)OpCode.CLEARITEMS,
            (byte)OpCode.LDLOC0, (byte)OpCode.DEC, (byte)OpCode.DUP,
            (byte)OpCode.STLOC0, (byte)OpCode.PUSH0,
            (byte)OpCode.JMPGT, 0xf6,
            (byte)OpCode.PUSH16, (byte)OpCode.PUSH7, (byte)OpCode.SHL, (byte)OpCode.PUSH14, (byte)OpCode.ADD, (byte)OpCode.STLOC0,
            (byte)OpCode.PUSH0,
            (byte)OpCode.LDLOC0, (byte)OpCode.DEC, (byte)OpCode.DUP,
            (byte)OpCode.STLOC0, (byte)OpCode.PUSH0,
            (byte)OpCode.JMPGT, 0xfa,
        ];

        using var engine = new TestEngine();
        engine.LoadScript(prog);
        Assert.AreEqual(VMState.FAULT, engine.Execute());
        Assert.IsNotNull(engine.FaultException);
        Assert.AreEqual($"MaxStackSize exceed: {ExecutionEngineLimits.Default.MaxStackSize + 1}/{ExecutionEngineLimits.Default.MaxStackSize}", engine.FaultException.Message);
        Assert.AreEqual((int)ExecutionEngineLimits.Default.MaxStackSize + 1, engine.ReferenceCounter.Count);
    }

    private delegate StackItem ContainerFactory(out int keyCount);

    private static (string Name, ContainerFactory Factory)[] UnpackContainers()
    {
        return
        [
            ("array", (out int keyCount) =>
            {
                keyCount = 0;
                return new Array(new StackItem[] { new Array(new StackItem[] { 42 }) });
            }),
            ("struct", (out int keyCount) =>
            {
                keyCount = 0;
                return new Struct(new StackItem[] { new Array(new StackItem[] { 42 }) });
            }),
            ("map", (out int keyCount) =>
            {
                keyCount = 1;
                return new Map { [new Integer(0)] = new Array(new StackItem[] { 42 }) };
            }),
        ];
    }

    private static (string Name, ContainerFactory Factory)[] ValuesContainers()
    {
        return
        [
            ("array", (out int keyCount) =>
            {
                keyCount = 0;
                return new Array(new StackItem[] { new Struct(new StackItem[] { 42 }) });
            }),
            ("struct", (out int keyCount) =>
            {
                keyCount = 0;
                return new Struct(new StackItem[] { new Struct(new StackItem[] { 42 }) });
            }),
            ("map", (out int keyCount) =>
            {
                keyCount = 1;
                return new Map { [new Integer(0)] = new Struct(new StackItem[] { 42 }) };
            }),
        ];
    }

    private static (string Name, ContainerFactory Factory)[] SetItemCloneContainers()
    {
        return
        [
            ("array", (out int keyCount) =>
            {
                keyCount = 0;
                return new Array(new StackItem[] { StackItem.Null });
            }),
            ("struct", (out int keyCount) =>
            {
                keyCount = 0;
                return new Struct(new StackItem[] { StackItem.Null });
            }),
            ("map", (out int keyCount) =>
            {
                keyCount = 1;
                return new Map { [new Integer(0)] = StackItem.Null };
            }),
        ];
    }
}
