// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ReferenceCounterComprehensive.cs file belongs to the neo project and is free
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
using System.Collections.Generic;
using Array = Neo.VM.Types.Array;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.Test;

/// <summary>
/// Comprehensive unit tests for ReferenceCounter to ensure behavioral consistency
/// when switching to a new RC implementation.
///
/// Test Categories:
/// 1. Basic Stack Reference Operations
/// 2. Zero-Referred Item Management
/// 3. Edge Cases and Boundary Conditions
/// 4. Type-Specific Tracking
/// 5. Cache Invalidation Scenarios
/// 6. VM Integration Tests
/// 7. Slot Integration Tests
/// 8. Stress Tests
/// 9. Behavioral Consistency Tests
/// 10. Additional Code Path Coverage Tests
/// </summary>
[TestClass]
public class UT_ReferenceCounterComprehensive
{
    #region 1. Basic Stack Reference Operations

    [TestMethod]
    public void TestAddStackReference_SingleItem_CountIncreases()
    {
        var rc = new ReferenceCounter();
        var array = new Array();

        Assert.AreEqual(0, rc.Count);
        rc.AddStackReference(array);
        Assert.AreEqual(1, rc.Count);
    }

    [TestMethod]
    public void TestAddStackReference_MultipleCount_CountIncreasesCorrectly()
    {
        var rc = new ReferenceCounter();
        var array = new Array();

        rc.AddStackReference(array, 5);
        Assert.AreEqual(5, rc.Count);
    }

    [TestMethod]
    public void TestRemoveStackReference_SingleItem_CountDecreases()
    {
        var rc = new ReferenceCounter();
        var array = new Array();

        rc.AddStackReference(array, 3);
        Assert.AreEqual(3, rc.Count);

        rc.RemoveStackReference(array);
        Assert.AreEqual(2, rc.Count);
    }

    [TestMethod]
    public void TestStackReference_NonTrackedType_CountStillChanges()
    {
        var rc = new ReferenceCounter();
        StackItem intItem = 42; // Integer - not tracked

        rc.AddStackReference(intItem);
        Assert.AreEqual(1, rc.Count);

        rc.RemoveStackReference(intItem);
        Assert.AreEqual(0, rc.Count);
    }

    [TestMethod]
    public void TestStackReference_Buffer_IsTracked()
    {
        var rc = new ReferenceCounter();
        var buffer = new Buffer(10);

        rc.AddStackReference(buffer);
        Assert.AreEqual(1, rc.Count);

        rc.AddStackReference(buffer);
        Assert.AreEqual(2, rc.Count);
    }

    #endregion

    #region 2. Zero-Referred Item Management

    [TestMethod]
    public void TestAddZeroReferred_NoTrackingElements()
    {
        var rc = new ReferenceCounter();
        var array = new Array();

        // Array creation itself doesn't imply RC modifications since no items on stack are expected.
        Assert.AreEqual(0, rc.Count);
    }

    [TestMethod]
    public void TestCheckZeroReferred_WithStackReference()
    {
        var rc = new ReferenceCounter();
        var array = new Array();

        rc.AddStackReference(array);

        Assert.AreEqual(1, rc.Count);
    }

    [TestMethod]
    public void TestCheckZeroReferred_NoZeroItems_ReturnsCurrentCount()
    {
        var rc = new ReferenceCounter();
        var array = new Array();

        rc.AddStackReference(array);
        rc.AddStackReference(array);

        Assert.AreEqual(2, rc.Count);
    }

    [TestMethod]
    public void TestRemoveStackReference_ToZero_AddsToZeroReferred()
    {
        var rc = new ReferenceCounter();
        var array = new Array();

        rc.AddStackReference(array);
        Assert.AreEqual(1, rc.Count);

        rc.RemoveStackReference(array);
        Assert.AreEqual(0, rc.Count); // Cleaned up
    }

    #endregion

    #region 3. Edge Cases and Boundary Conditions

    [TestMethod]
    public void TestEmptyReferenceCounter_CountIsZero()
    {
        var rc = new ReferenceCounter();
        Assert.AreEqual(0, rc.Count);
    }

    [TestMethod]
    public void TestLargeNumberOfItems()
    {
        var rc = new ReferenceCounter();
        var arrays = new Array[100];

        for (int i = 0; i < 100; i++)
        {
            arrays[i] = new Array();
            rc.AddStackReference(arrays[i]);
        }

        Assert.AreEqual(100, rc.Count);

        for (int i = 0; i < 100; i++)
        {
            rc.RemoveStackReference(arrays[i]);
        }

        Assert.AreEqual(0, rc.Count);
    }

    [TestMethod]
    public void TestDeepNesting()
    {
        var rc = new ReferenceCounter();
        var root = new Array();
        rc.AddStackReference(root);

        var current = root;
        for (int i = 0; i < 50; i++)
        {
            var next = new Array();
            current.Add(next);
            rc.AddStackReference(next);
            current = next;
        }

        Assert.AreEqual(51, rc.Count); // root + 50 nested

        rc.RemoveStackReference(root);

        Assert.AreEqual(0, rc.Count);
    }

    #endregion

    #region 4. Type-Specific Tracking

    [TestMethod]
    public void TestArray_TrackedCorrectly()
    {
        var rc = new ReferenceCounter();
        var array = new Array();

        rc.AddStackReference(array);
        Assert.AreEqual(1, rc.Count);

        array.Add(1);
        rc.AddStackReference(1);
        array.Add(2);
        rc.AddStackReference(2);
        array.Add(3);
        rc.AddStackReference(3);

        Assert.AreEqual(4, rc.Count);
    }

    [TestMethod]
    public void TestMap_TrackedCorrectly()
    {
        var rc = new ReferenceCounter();
        var map = new Map();

        rc.AddStackReference(map);
        Assert.AreEqual(1, rc.Count);

        map[(ByteString)"key1"] = 1;
        rc.AddStackReference("key1");
        rc.AddStackReference(1);
        map[(ByteString)"key2"] = 2;
        rc.AddStackReference("key2");
        rc.AddStackReference(2);

        // Map tracks both keys and values
        Assert.AreEqual(5, rc.Count); // 1 (map) + 2 keys + 2 values
    }

    [TestMethod]
    public void TestStruct_TrackedCorrectly()
    {
        var rc = new ReferenceCounter();
        var s = new Struct();

        rc.AddStackReference(s);
        Assert.AreEqual(1, rc.Count);

        s.Add(1);
        rc.AddStackReference(1);
        s.Add(2);
        rc.AddStackReference(2);

        Assert.AreEqual(3, rc.Count);
    }

    [TestMethod]
    public void TestBuffer_TrackedCorrectly()
    {
        var rc = new ReferenceCounter();
        var buffer = new Buffer(10);

        rc.AddStackReference(buffer);
        Assert.AreEqual(1, rc.Count);

        rc.RemoveStackReference(buffer);

        Assert.AreEqual(0, rc.Count);
    }

    [TestMethod]
    public void TestNestedCompoundTypes()
    {
        var rc = new ReferenceCounter();
        var array = new Array();
        var map = new Map();
        var s = new Struct();

        rc.AddStackReference(array);

        array.Add(map);
        rc.AddStackReference(map);

        var key = (ByteString)"struct";
        map[key] = s;
        rc.AddStackReference(key);
        rc.AddStackReference(s);

        var val = 42;
        s.Add(val);
        rc.AddStackReference(val);

        // array(1) + map(1) + key(1) + struct(1) + int(1) = 5
        Assert.AreEqual(5, rc.Count);
    }

    #endregion

    #region 5. Cache Invalidation Scenarios

    [TestMethod]
    public void TestCacheInvalidation_AddReference()
    {
        var rc = new ReferenceCounter();
        var parent = new Array();
        var child = new Array();

        rc.AddStackReference(parent);
        rc.AddStackReference(child);

        // Add reference should invalidate cache
        parent.Add(child);
        rc.AddStackReference(child);

        rc.RemoveStackReference(parent);
        rc.RemoveStackReference(child);

        Assert.AreEqual(0, rc.Count);
    }

    [TestMethod]
    public void TestCacheInvalidation_RemoveReference()
    {
        var rc = new ReferenceCounter();
        var parent = new Array();
        var child = new Array();

        rc.AddStackReference(parent);
        parent.Add(child);
        rc.AddStackReference(child);

        // Remove child from parent
        var item = parent[0];
        parent.RemoveAt(0);
        rc.RemoveStackReference(item);

        rc.RemoveStackReference(parent);

        Assert.AreEqual(0, rc.Count);
    }

    #endregion

    #region 6. VM Integration Tests

    [TestMethod]
    public void TestVMIntegration_SimpleScript()
    {
        using ScriptBuilder sb = new();
        sb.EmitPush(0);
        sb.Emit(OpCode.NEWARRAY);
        sb.Emit(OpCode.DROP);
        sb.Emit(OpCode.RET);

        using ExecutionEngine engine = new();
        engine.LoadScript(sb.ToArray());

        Assert.AreEqual(VMState.HALT, engine.Execute());
        Assert.AreEqual(0, engine.ReferenceCounter.Count);
    }

    [TestMethod]
    public void TestVMIntegration_ArrayWithItems()
    {
        using ScriptBuilder sb = new();
        sb.EmitPush(3);
        sb.Emit(OpCode.NEWARRAY);

        using ExecutionEngine engine = new();
        engine.LoadScript(sb.ToArray());

        Assert.AreEqual(VMState.HALT, engine.Execute());
        // Array + 3 null items on stack
        Assert.AreEqual(4, engine.ReferenceCounter.Count);
    }

    [TestMethod]
    public void TestVMIntegration_NestedArrays()
    {
        using ScriptBuilder sb = new();
        sb.EmitPush(1);
        sb.Emit(OpCode.NEWARRAY); // Create outer array
        sb.Emit(OpCode.DUP);
        sb.EmitPush(0);
        sb.EmitPush(0);
        sb.Emit(OpCode.NEWARRAY); // Create inner array
        sb.Emit(OpCode.SETITEM); // outer[0] = inner

        using ExecutionEngine engine = new();
        engine.LoadScript(sb.ToArray());

        Assert.AreEqual(VMState.HALT, engine.Execute());
        // Outer array + inner array
        Assert.AreEqual(2, engine.ReferenceCounter.Count);
    }

    [TestMethod]
    public void TestVMIntegration_MapOperations()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.NEWMAP);
        sb.Emit(OpCode.DUP);
        sb.EmitPush("key");
        sb.EmitPush(42);
        sb.Emit(OpCode.SETITEM);

        using ExecutionEngine engine = new();
        engine.LoadScript(sb.ToArray());

        Assert.AreEqual(VMState.HALT, engine.Execute());
        // Map + key + value
        Assert.AreEqual(3, engine.ReferenceCounter.Count);
    }

    [TestMethod]
    public void TestVMIntegration_StructClone()
    {
        using ScriptBuilder sb = new();
        sb.EmitPush(2);
        sb.Emit(OpCode.NEWSTRUCT);
        sb.Emit(OpCode.DUP);
        sb.EmitPush(0);
        sb.EmitPush(100);
        sb.Emit(OpCode.SETITEM);
        sb.Emit(OpCode.DUP);
        sb.EmitPush(1);
        sb.EmitPush(200);
        sb.Emit(OpCode.SETITEM);

        using ExecutionEngine engine = new();
        engine.LoadScript(sb.ToArray());

        Assert.AreEqual(VMState.HALT, engine.Execute());
        // Struct + 2 values
        Assert.AreEqual(3, engine.ReferenceCounter.Count);
    }

    #endregion

    #region 7. Slot Integration Tests

    [TestMethod]
    public void TestSlot_InitializesWithNullReferences()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.INITSLOT, new byte[] { 3, 0 }); // 3 local variables

        using ExecutionEngine engine = new();
        engine.LoadScript(sb.ToArray());

        Assert.AreEqual(VMState.HALT, engine.Execute());
        // Null is not tracked (not CompoundType or Buffer), so count is 0
        // This verifies that non-tracked types don't affect the reference count
        Assert.AreEqual(0, engine.ReferenceCounter.Count);
    }

    [TestMethod]
    public void TestSlot_SetAndGet()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.INITSLOT, new byte[] { 1, 0 });
        sb.EmitPush(0);
        sb.Emit(OpCode.NEWARRAY);
        sb.Emit(OpCode.STLOC0);
        sb.Emit(OpCode.LDLOC0);

        using ExecutionEngine engine = new();
        engine.LoadScript(sb.ToArray());

        Assert.AreEqual(VMState.HALT, engine.Execute());
        // Array in slot (1 ref) + array on stack (1 ref) = 2 stack refs for same array
        // But the array itself is only counted once per stack reference
        // After execution: array is on stack (1 ref from stack)
        Assert.AreEqual(1, engine.ReferenceCounter.Count);
    }

    [TestMethod]
    public void TestStaticSlot_PersistsAcrossContexts()
    {
        using ScriptBuilder sb = new();
        sb.Emit(OpCode.INITSSLOT, new byte[] { 1 });
        sb.EmitPush(0);
        sb.Emit(OpCode.NEWARRAY);
        sb.Emit(OpCode.STSFLD0);
        sb.Emit(OpCode.LDSFLD0);

        using ExecutionEngine engine = new();
        engine.LoadScript(sb.ToArray());

        Assert.AreEqual(VMState.HALT, engine.Execute());
        // Array on stack (1 ref) - static slot is cleared after execution
        Assert.AreEqual(1, engine.ReferenceCounter.Count);
    }

    #endregion

    #region 8. Stress Tests

    [TestMethod]
    public void TestStress_ManySmallArrays()
    {
        var rc = new ReferenceCounter();
        var arrays = new Array[1000];

        for (int i = 0; i < 1000; i++)
        {
            arrays[i] = new Array();
            rc.AddStackReference(arrays[i]);
        }

        Assert.AreEqual(1000, rc.Count);

        for (int i = 0; i < 1000; i++)
        {
            rc.RemoveStackReference(arrays[i]);
        }

        Assert.AreEqual(0, rc.Count);
    }

    [TestMethod]
    public void TestStress_ComplexInterconnectedGraph()
    {
        var rc = new ReferenceCounter();
        var arrays = new Array[20];

        for (int i = 0; i < 20; i++)
            arrays[i] = new Array();

        // Create complex interconnections
        for (int i = 0; i < 20; i++)
        {
            arrays[i].Add(arrays[(i + 1) % 20]);
            arrays[i].Add(arrays[(i + 5) % 20]);
        }

        for (int i = 0; i < 20; i++)
        {
            rc.AddStackReference(arrays[i]);
        }

        // Remove all stack references
        for (int i = 0; i < 20; i++)
        {
            rc.RemoveStackReference(arrays[i]);
        }

        Assert.IsPositive(rc.Count); // cyclic references were present, so the resulting value expected not to be 0.
    }

    [TestMethod]
    public void TestStress_RepeatedCheckZeroReferred()
    {
        var rc = new ReferenceCounter();
        var array = new Array();

        rc.AddStackReference(array);

        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual(1, rc.Count);
        }
    }

    #endregion

    #region 9. Behavioral Consistency Tests

    [TestMethod]
    public void TestBehavior_AddRemoveSymmetry()
    {
        var rc = new ReferenceCounter();
        var array = new Array();

        for (int i = 0; i < 10; i++)
        {
            rc.AddStackReference(array);
        }

        Assert.AreEqual(10, rc.Count);

        for (int i = 0; i < 10; i++)
        {
            rc.RemoveStackReference(array);
        }

        Assert.AreEqual(0, rc.Count);
    }

    [TestMethod]
    public void TestBehavior_CountNeverNegative()
    {
        var rc = new ReferenceCounter();

        // Even with no items, count should be 0
        Assert.AreEqual(0, rc.Count);

        var array = new Array();
        rc.AddStackReference(array);
        rc.RemoveStackReference(array);

        // After cleanup
        Assert.IsGreaterThanOrEqualTo(0, rc.Count);
    }

    [TestMethod]
    public void TestBehavior_DeterministicCleanup()
    {
        // Run the same scenario multiple times to ensure deterministic behavior
        for (int run = 0; run < 10; run++)
        {
            var rc = new ReferenceCounter();
            var a = new Array();
            var b = new Array();
            var c = new Array();

            a.Add(b);
            b.Add(c);
            c.Add(a);

            rc.AddStackReference(a);
            rc.RemoveStackReference(a);

            Assert.AreEqual(3, rc.Count, $"Run {run} failed"); // cyclic references were present, the resulting RC value must not be 0.
        }
    }

    #endregion

    #region 10. Additional Code Path Coverage Tests

    /// <summary>
    /// Tests AddStackReference when item already added to the stack.
    /// </summary>
    [TestMethod]
    public void TestAddStackReference_ExistingItem()
    {
        var rc = new ReferenceCounter();
        var array = new Array();

        rc.AddStackReference(array);
        rc.AddStackReference(array);
        Assert.AreEqual(2, rc.Count);

        rc.RemoveStackReference(array);
        rc.RemoveStackReference(array);
        Assert.AreEqual(0, rc.Count);
    }

    /// <summary>
    /// Tests AddStackReference properly updates subitems refs.
    /// </summary>
    [TestMethod]
    public void TestAddStackReference_RemovesFromZeroReferred()
    {
        var rc = new ReferenceCounter();
        var array = new Array(new List<StackItem>() { new Boolean(true) });

        rc.AddStackReference(array);

        // Remove and check - no reference should be left.
        rc.RemoveStackReference(array);
        Assert.AreEqual(0, rc.Count);

        // Add again - should update refs.
        rc.AddStackReference(array);
        Assert.AreEqual(2, rc.Count);
    }

    /// <summary>
    /// Tests AddStackReference with primitive type.
    /// </summary>
    [TestMethod]
    public void TestAddStackReference_PrimitiveType()
    {
        var rc = new ReferenceCounter();
        StackItem intItem = 42;

        rc.AddStackReference(intItem);
        Assert.AreEqual(1, rc.Count);
    }

    /// <summary>
    /// Tests RemoveStackReference when the last reference on compound type
    /// is removed from stack.
    /// </summary>
    [TestMethod]
    public void TestRemoveStackReference_RemoveLastReference()
    {
        var rc = new ReferenceCounter();
        StackItem intItem = 42;

        rc.AddStackReference(intItem);
        Assert.AreEqual(1, rc.Count);

        var parent = new Array { intItem };

        rc.AddStackReference(parent);
        Assert.AreEqual(3, rc.Count);

        rc.RemoveStackReference(parent);
        Assert.AreEqual(1, rc.Count);
    }

    /// <summary>
    /// Tests RemoveStackReference when item still has references.
    /// </summary>
    [TestMethod]
    public void TestRemoveStackReference_ItemStillHasStackReferences2()
    {
        var rc = new ReferenceCounter();
        var array = new Array();

        rc.AddStackReference(array, 3); // Add 3 stack references
        Assert.AreEqual(3, rc.Count);

        rc.RemoveStackReference(array); // Remove 1, still has 2
        Assert.AreEqual(2, rc.Count);
    }

    /// <summary>
    /// Slot indexer should replace references atomically, decrementing the old item and incrementing the new one.
    /// </summary>
    [TestMethod]
    public void TestSlotUpdateReplacesReferences()
    {
        var rc = new ReferenceCounter();
        var slot = new Slot(1, rc);
        var first = new Array();
        var second = new Array();

        // Constructor adds a stack reference for Null
        Assert.AreEqual(1, rc.Count);

        slot[0] = first;
        Assert.AreEqual(1, rc.Count);

        slot[0] = second;
        Assert.AreEqual(1, rc.Count);

        rc.RemoveStackReference(second);
        Assert.AreEqual(0, rc.Count);
    }

    #endregion
}
