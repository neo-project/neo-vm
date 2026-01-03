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
using Array = Neo.VM.Types.Array;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.Test;

/// <summary>
/// Comprehensive unit tests for ReferenceCounter to ensure behavioral consistency
/// when switching to a new RC implementation.
///
/// Test Categories:
/// 1. Basic Stack Reference Operations
/// 2. Object Reference Operations (CompoundType relationships)
/// 3. Zero-Referred Item Management
/// 4. Circular Reference Detection (Tarjan SCC)
/// 5. Mixed Reference Scenarios
/// 6. Edge Cases and Boundary Conditions
/// 7. Type-Specific Tracking (Array, Map, Struct, Buffer)
/// 8. Cache Invalidation Scenarios
/// </summary>
[TestClass]
public class UT_ReferenceCounterComprehensive
{
    #region 1. Basic Stack Reference Operations

    [TestMethod]
    public void TestAddStackReference_SingleItem_CountIncreases()
    {
        var rc = new ReferenceCounter();
        var array = new Array(rc);

        Assert.AreEqual(0, rc.Count);
        rc.AddStackReference(array);
        Assert.AreEqual(1, rc.Count);
    }

    [TestMethod]
    public void TestAddStackReference_MultipleCount_CountIncreasesCorrectly()
    {
        var rc = new ReferenceCounter();
        var array = new Array(rc);

        rc.AddStackReference(array, 5);
        Assert.AreEqual(5, rc.Count);
    }

    [TestMethod]
    public void TestRemoveStackReference_SingleItem_CountDecreases()
    {
        var rc = new ReferenceCounter();
        var array = new Array(rc);

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

    #region 2. Object Reference Operations

    [TestMethod]
    public void TestAddReference_ParentChild_CountIncreases()
    {
        var rc = new ReferenceCounter();
        var parent = new Array(rc);
        var child = new Array(rc);

        rc.AddStackReference(parent);
        Assert.AreEqual(1, rc.Count);

        rc.AddReference(child, parent);
        Assert.AreEqual(2, rc.Count);
    }

    [TestMethod]
    public void TestRemoveReference_ParentChild_CountDecreases()
    {
        var rc = new ReferenceCounter();
        var parent = new Array(rc);
        var child = new Array(rc);

        rc.AddStackReference(parent);
        rc.AddReference(child, parent);
        Assert.AreEqual(2, rc.Count);

        rc.RemoveReference(child, parent);
        Assert.AreEqual(1, rc.Count);
    }

    [TestMethod]
    public void TestAddReference_MultipleParents_TracksCorrectly()
    {
        var rc = new ReferenceCounter();
        var parent1 = new Array(rc);
        var parent2 = new Array(rc);
        var child = new Array(rc);

        rc.AddStackReference(parent1);
        rc.AddStackReference(parent2);
        rc.AddReference(child, parent1);
        rc.AddReference(child, parent2);

        Assert.AreEqual(4, rc.Count);
    }

    [TestMethod]
    public void TestAddReference_SameParentMultipleTimes_TracksCorrectly()
    {
        var rc = new ReferenceCounter();
        var parent = new Array(rc);
        var child = new Array(rc);

        rc.AddStackReference(parent);
        rc.AddReference(child, parent);
        rc.AddReference(child, parent);
        rc.AddReference(child, parent);

        Assert.AreEqual(4, rc.Count);
    }

    [TestMethod]
    public void TestAddReference_NonTrackedChild_CountStillIncreases()
    {
        var rc = new ReferenceCounter();
        var parent = new Array(rc);
        StackItem intChild = 42;

        rc.AddStackReference(parent);
        rc.AddReference(intChild, parent);

        Assert.AreEqual(2, rc.Count);
    }

    #endregion

    #region 3. Zero-Referred Item Management

    [TestMethod]
    public void TestAddZeroReferred_AddsToTracking()
    {
        var rc = new ReferenceCounter();
        var array = new Array(rc);

        // Array constructor calls AddZeroReferred
        // CheckZeroReferred should clean it up since it has no references
        int count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public void TestCheckZeroReferred_WithStackReference_NotCleaned()
    {
        var rc = new ReferenceCounter();
        var array = new Array(rc);

        rc.AddStackReference(array);
        int count = rc.CheckZeroReferred();

        Assert.AreEqual(1, count);
    }

    [TestMethod]
    public void TestCheckZeroReferred_NoZeroItems_ReturnsCurrentCount()
    {
        var rc = new ReferenceCounter();
        var array = new Array(rc);

        rc.AddStackReference(array);
        rc.AddStackReference(array);

        int count = rc.CheckZeroReferred();
        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public void TestRemoveStackReference_ToZero_AddsToZeroReferred()
    {
        var rc = new ReferenceCounter();
        var array = new Array(rc);

        rc.AddStackReference(array);
        Assert.AreEqual(1, rc.Count);

        rc.RemoveStackReference(array);
        // Item should be in zero-referred list now
        int count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count); // Cleaned up
    }

    #endregion

    #region 4. Circular Reference Detection

    [TestMethod]
    public void TestCircularReference_SelfReference_CleanedUp()
    {
        var rc = new ReferenceCounter();
        var array = new Array(rc);

        rc.AddStackReference(array);
        array.Add(array); // Self-reference

        Assert.AreEqual(2, rc.Count);

        rc.RemoveStackReference(array);
        int count = rc.CheckZeroReferred();

        // Self-referencing array should be cleaned up
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public void TestCircularReference_TwoItems_CleanedUp()
    {
        var rc = new ReferenceCounter();
        var array1 = new Array(rc);
        var array2 = new Array(rc);

        rc.AddStackReference(array1);
        rc.AddStackReference(array2);

        array1.Add(array2);
        array2.Add(array1);

        Assert.AreEqual(4, rc.Count);

        rc.RemoveStackReference(array1);
        rc.RemoveStackReference(array2);

        int count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public void TestCircularReference_ThreeItems_CleanedUp()
    {
        var rc = new ReferenceCounter();
        var a = new Array(rc);
        var b = new Array(rc);
        var c = new Array(rc);

        rc.AddStackReference(a);

        a.Add(b);
        b.Add(c);
        c.Add(a); // Creates cycle: a -> b -> c -> a

        rc.RemoveStackReference(a);
        int count = rc.CheckZeroReferred();

        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public void TestCircularReference_PartiallyReachable_NotCleanedUp()
    {
        var rc = new ReferenceCounter();
        var root = new Array(rc);
        var a = new Array(rc);
        var b = new Array(rc);

        rc.AddStackReference(root);

        root.Add(a);
        a.Add(b);
        b.Add(a); // Cycle between a and b, but reachable from root

        int count = rc.CheckZeroReferred();
        Assert.AreEqual(4, count); // root + a + b + reference from root to a
    }

    [TestMethod]
    public void TestCircularReference_ComplexGraph_CleanedUpCorrectly()
    {
        var rc = new ReferenceCounter();
        var arrays = new Array[5];

        for (int i = 0; i < 5; i++)
        {
            arrays[i] = new Array(rc);
            rc.AddStackReference(arrays[i]);
        }

        // Create complex graph: 0->1, 1->2, 2->3, 3->4, 4->0, 2->0
        arrays[0].Add(arrays[1]);
        arrays[1].Add(arrays[2]);
        arrays[2].Add(arrays[3]);
        arrays[3].Add(arrays[4]);
        arrays[4].Add(arrays[0]);
        arrays[2].Add(arrays[0]);

        // Remove all stack references
        for (int i = 0; i < 5; i++)
        {
            rc.RemoveStackReference(arrays[i]);
        }

        int count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    #endregion

    #region 5. Mixed Reference Scenarios

    [TestMethod]
    public void TestMixedReferences_StackAndObject()
    {
        var rc = new ReferenceCounter();
        var parent = new Array(rc);
        var child = new Array(rc);

        rc.AddStackReference(parent);
        rc.AddStackReference(child);
        parent.Add(child);

        Assert.AreEqual(3, rc.Count);

        rc.RemoveStackReference(child);
        int count = rc.CheckZeroReferred();

        // Child still reachable through parent
        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public void TestMixedReferences_RemoveParentFirst()
    {
        var rc = new ReferenceCounter();
        var parent = new Array(rc);
        var child = new Array(rc);

        rc.AddStackReference(parent);
        parent.Add(child);

        rc.RemoveStackReference(parent);
        int count = rc.CheckZeroReferred();

        // Both should be cleaned up
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public void TestMixedReferences_NestedArrays()
    {
        var rc = new ReferenceCounter();
        var level1 = new Array(rc);
        var level2 = new Array(rc);
        var level3 = new Array(rc);

        rc.AddStackReference(level1);
        level1.Add(level2);
        level2.Add(level3);

        Assert.AreEqual(3, rc.Count);

        rc.RemoveStackReference(level1);
        int count = rc.CheckZeroReferred();

        Assert.AreEqual(0, count);
    }

    #endregion

    #region 6. Edge Cases and Boundary Conditions

    [TestMethod]
    public void TestEmptyReferenceCounter_CountIsZero()
    {
        var rc = new ReferenceCounter();
        Assert.AreEqual(0, rc.Count);
    }

    [TestMethod]
    public void TestCheckZeroReferred_EmptyCounter_ReturnsZero()
    {
        var rc = new ReferenceCounter();
        int count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public void TestCheckZeroReferred_CalledMultipleTimes_Idempotent()
    {
        var rc = new ReferenceCounter();
        var array = new Array(rc);

        rc.AddStackReference(array);

        int count1 = rc.CheckZeroReferred();
        int count2 = rc.CheckZeroReferred();
        int count3 = rc.CheckZeroReferred();

        Assert.AreEqual(count1, count2);
        Assert.AreEqual(count2, count3);
    }

    [TestMethod]
    public void TestLargeNumberOfItems()
    {
        var rc = new ReferenceCounter();
        var arrays = new Array[100];

        for (int i = 0; i < 100; i++)
        {
            arrays[i] = new Array(rc);
            rc.AddStackReference(arrays[i]);
        }

        Assert.AreEqual(100, rc.Count);

        for (int i = 0; i < 100; i++)
        {
            rc.RemoveStackReference(arrays[i]);
        }

        int count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public void TestDeepNesting()
    {
        var rc = new ReferenceCounter();
        var root = new Array(rc);
        rc.AddStackReference(root);

        var current = root;
        for (int i = 0; i < 50; i++)
        {
            var next = new Array(rc);
            current.Add(next);
            current = next;
        }

        Assert.AreEqual(51, rc.Count); // root + 50 nested

        rc.RemoveStackReference(root);
        int count = rc.CheckZeroReferred();

        Assert.AreEqual(0, count);
    }

    #endregion

    #region 7. Type-Specific Tracking

    [TestMethod]
    public void TestArray_TrackedCorrectly()
    {
        var rc = new ReferenceCounter();
        var array = new Array(rc);

        rc.AddStackReference(array);
        Assert.AreEqual(1, rc.Count);

        array.Add(1);
        array.Add(2);
        array.Add(3);

        Assert.AreEqual(4, rc.Count);
    }

    [TestMethod]
    public void TestMap_TrackedCorrectly()
    {
        var rc = new ReferenceCounter();
        var map = new Map(rc);

        rc.AddStackReference(map);
        Assert.AreEqual(1, rc.Count);

        map[(ByteString)"key1"] = 1;
        map[(ByteString)"key2"] = 2;

        // Map tracks both keys and values
        Assert.AreEqual(5, rc.Count); // 1 (map) + 2 keys + 2 values
    }

    [TestMethod]
    public void TestStruct_TrackedCorrectly()
    {
        var rc = new ReferenceCounter();
        var s = new Struct(rc);

        rc.AddStackReference(s);
        Assert.AreEqual(1, rc.Count);

        s.Add(1);
        s.Add(2);

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
        int count = rc.CheckZeroReferred();

        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public void TestNestedCompoundTypes()
    {
        var rc = new ReferenceCounter();
        var array = new Array(rc);
        var map = new Map(rc);
        var s = new Struct(rc);

        rc.AddStackReference(array);

        array.Add(map);
        map[(ByteString)"struct"] = s;
        s.Add(42);

        // array(1) + map(1) + key(1) + struct(1) + int(1) = 5
        Assert.AreEqual(5, rc.Count);
    }

    [TestMethod]
    public void TestArray_Clear_RemovesReferences()
    {
        var rc = new ReferenceCounter();
        var array = new Array(rc);

        rc.AddStackReference(array);
        array.Add(1);
        array.Add(2);
        array.Add(3);

        Assert.AreEqual(4, rc.Count);

        array.Clear();
        Assert.AreEqual(1, rc.Count);
    }

    [TestMethod]
    public void TestArray_RemoveAt_RemovesReference()
    {
        var rc = new ReferenceCounter();
        var array = new Array(rc);

        rc.AddStackReference(array);
        array.Add(1);
        array.Add(2);
        array.Add(3);

        Assert.AreEqual(4, rc.Count);

        array.RemoveAt(1);
        Assert.AreEqual(3, rc.Count);
    }

    [TestMethod]
    public void TestArray_SetItem_UpdatesReferences()
    {
        var rc = new ReferenceCounter();
        var array = new Array(rc);

        rc.AddStackReference(array);
        array.Add(1);

        Assert.AreEqual(2, rc.Count);

        array[0] = 2; // Replace item
        Assert.AreEqual(2, rc.Count); // Count should remain same
    }

    [TestMethod]
    public void TestMap_Remove_RemovesReferences()
    {
        var rc = new ReferenceCounter();
        var map = new Map(rc);

        rc.AddStackReference(map);
        map[(ByteString)"key"] = 42;

        Assert.AreEqual(3, rc.Count); // map + key + value

        map.Remove((ByteString)"key");
        Assert.AreEqual(1, rc.Count); // only map
    }

    [TestMethod]
    public void TestMap_Clear_RemovesAllReferences()
    {
        var rc = new ReferenceCounter();
        var map = new Map(rc);

        rc.AddStackReference(map);
        map[(ByteString)"key1"] = 1;
        map[(ByteString)"key2"] = 2;

        Assert.AreEqual(5, rc.Count);

        map.Clear();
        Assert.AreEqual(1, rc.Count);
    }

    #endregion

    #region 8. Cache Invalidation Scenarios

    [TestMethod]
    public void TestCacheInvalidation_AddReference()
    {
        var rc = new ReferenceCounter();
        var parent = new Array(rc);
        var child = new Array(rc);

        rc.AddStackReference(parent);
        rc.AddStackReference(child);

        // First check builds cache
        rc.CheckZeroReferred();

        // Add reference should invalidate cache
        parent.Add(child);

        rc.RemoveStackReference(parent);
        rc.RemoveStackReference(child);

        int count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public void TestCacheInvalidation_RemoveReference()
    {
        var rc = new ReferenceCounter();
        var parent = new Array(rc);
        var child = new Array(rc);

        rc.AddStackReference(parent);
        parent.Add(child);

        rc.CheckZeroReferred();

        // Remove child from parent
        parent.RemoveAt(0);

        rc.RemoveStackReference(parent);

        int count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    #endregion

    #region 9. VM Integration Tests

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

    #region 10. Slot Integration Tests

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

    #region 11. Stress Tests

    [TestMethod]
    public void TestStress_ManySmallArrays()
    {
        var rc = new ReferenceCounter();
        var arrays = new Array[1000];

        for (int i = 0; i < 1000; i++)
        {
            arrays[i] = new Array(rc);
            rc.AddStackReference(arrays[i]);
        }

        Assert.AreEqual(1000, rc.Count);

        for (int i = 0; i < 1000; i++)
        {
            rc.RemoveStackReference(arrays[i]);
        }

        int count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public void TestStress_ComplexInterconnectedGraph()
    {
        var rc = new ReferenceCounter();
        var arrays = new Array[20];

        for (int i = 0; i < 20; i++)
        {
            arrays[i] = new Array(rc);
            rc.AddStackReference(arrays[i]);
        }

        // Create complex interconnections
        for (int i = 0; i < 20; i++)
        {
            arrays[i].Add(arrays[(i + 1) % 20]);
            arrays[i].Add(arrays[(i + 5) % 20]);
        }

        // Remove all stack references
        for (int i = 0; i < 20; i++)
        {
            rc.RemoveStackReference(arrays[i]);
        }

        int count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public void TestStress_RepeatedCheckZeroReferred()
    {
        var rc = new ReferenceCounter();
        var array = new Array(rc);

        rc.AddStackReference(array);

        for (int i = 0; i < 100; i++)
        {
            int count = rc.CheckZeroReferred();
            Assert.AreEqual(1, count);
        }
    }

    #endregion

    #region 12. Behavioral Consistency Tests

    [TestMethod]
    public void TestBehavior_AddRemoveSymmetry()
    {
        var rc = new ReferenceCounter();
        var array = new Array(rc);

        for (int i = 0; i < 10; i++)
        {
            rc.AddStackReference(array);
        }

        Assert.AreEqual(10, rc.Count);

        for (int i = 0; i < 10; i++)
        {
            rc.RemoveStackReference(array);
        }

        int count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public void TestBehavior_ObjectReferenceSymmetry()
    {
        var rc = new ReferenceCounter();
        var parent = new Array(rc);
        var child = new Array(rc);

        rc.AddStackReference(parent);

        for (int i = 0; i < 5; i++)
        {
            rc.AddReference(child, parent);
        }

        Assert.AreEqual(6, rc.Count);

        for (int i = 0; i < 5; i++)
        {
            rc.RemoveReference(child, parent);
        }

        rc.RemoveStackReference(parent);

        int count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public void TestBehavior_CountNeverNegative()
    {
        var rc = new ReferenceCounter();

        // Even with no items, count should be 0
        Assert.AreEqual(0, rc.Count);

        var array = new Array(rc);
        rc.AddStackReference(array);
        rc.RemoveStackReference(array);

        // After cleanup
        rc.CheckZeroReferred();
        Assert.IsGreaterThanOrEqualTo(0, rc.Count);
    }

    [TestMethod]
    public void TestBehavior_DeterministicCleanup()
    {
        // Run the same scenario multiple times to ensure deterministic behavior
        for (int run = 0; run < 10; run++)
        {
            var rc = new ReferenceCounter();
            var a = new Array(rc);
            var b = new Array(rc);
            var c = new Array(rc);

            rc.AddStackReference(a);
            a.Add(b);
            b.Add(c);
            c.Add(a);

            rc.RemoveStackReference(a);
            int count = rc.CheckZeroReferred();

            Assert.AreEqual(0, count, $"Run {run} failed");
        }
    }

    #endregion

    #region 13. Additional Code Path Coverage Tests

    /// <summary>
    /// Tests AddStackReference when item already exists in _trackedItems
    /// and _cachedComponents is not null (line 100-101)
    /// </summary>
    [TestMethod]
    public void TestAddStackReference_ExistingItem_CachedComponentsNotNull()
    {
        var rc = new ReferenceCounter();
        var array = new Array(rc);

        rc.AddStackReference(array);
        // First CheckZeroReferred builds _cachedComponents
        rc.CheckZeroReferred();

        // Add another stack reference to existing item - should not add to cached components again
        rc.AddStackReference(array);
        Assert.AreEqual(2, rc.Count);

        // Verify cleanup still works correctly
        rc.RemoveStackReference(array);
        rc.RemoveStackReference(array);
        int count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Tests AddStackReference removing item from _zeroReferred (line 107)
    /// </summary>
    [TestMethod]
    public void TestAddStackReference_RemovesFromZeroReferred()
    {
        var rc = new ReferenceCounter();
        var array = new Array(rc);

        // Array is in _zeroReferred after construction
        // Adding stack reference should remove it from _zeroReferred
        rc.AddStackReference(array);

        // Remove and check - should be added back to _zeroReferred
        rc.RemoveStackReference(array);

        // Add again - should remove from _zeroReferred again
        rc.AddStackReference(array);

        // Now cleanup should not remove the array
        int count = rc.CheckZeroReferred();
        Assert.AreEqual(1, count);
    }

    /// <summary>
    /// Tests AddZeroReferred with non-tracked type (line 117)
    /// </summary>
    [TestMethod]
    public void TestAddZeroReferred_NonTrackedType()
    {
        var rc = new ReferenceCounter();
        StackItem intItem = 42;

        // Directly call AddZeroReferred on non-tracked type
        rc.AddZeroReferred(intItem);

        // Should not affect count since it's not tracked
        int count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Tests AddZeroReferred when _cachedComponents exists (line 120)
    /// </summary>
    [TestMethod]
    public void TestAddZeroReferred_WithCachedComponents()
    {
        var rc = new ReferenceCounter();
        var array1 = new Array(rc);

        rc.AddStackReference(array1);
        // Build cached components
        rc.CheckZeroReferred();

        // Create new array - AddZeroReferred is called in constructor
        // This should add to _cachedComponents
        var array2 = new Array(rc);
        rc.AddStackReference(array2);

        // Both should be tracked
        Assert.AreEqual(2, rc.Count);

        rc.RemoveStackReference(array1);
        rc.RemoveStackReference(array2);
        int count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Tests RemoveReference with non-tracked type (line 223)
    /// </summary>
    [TestMethod]
    public void TestRemoveReference_NonTrackedType()
    {
        var rc = new ReferenceCounter();
        var parent = new Array(rc);
        StackItem intChild = 42;

        rc.AddStackReference(parent);
        rc.AddReference(intChild, parent);
        Assert.AreEqual(2, rc.Count);

        // Remove reference to non-tracked type
        rc.RemoveReference(intChild, parent);
        Assert.AreEqual(1, rc.Count);
    }

    /// <summary>
    /// Tests RemoveReference when item still has stack references (line 232-233)
    /// Item should NOT be added to _zeroReferred
    /// </summary>
    [TestMethod]
    public void TestRemoveReference_ItemStillHasStackReferences()
    {
        var rc = new ReferenceCounter();
        var parent = new Array(rc);
        var child = new Array(rc);

        rc.AddStackReference(parent);
        rc.AddStackReference(child); // Child has stack reference
        rc.AddReference(child, parent);

        Assert.AreEqual(3, rc.Count);

        // Remove object reference - child still has stack reference
        rc.RemoveReference(child, parent);
        Assert.AreEqual(2, rc.Count);

        // Child should NOT be in _zeroReferred, so CheckZeroReferred should not clean it
        int count = rc.CheckZeroReferred();
        Assert.AreEqual(2, count); // Both parent and child still exist
    }

    /// <summary>
    /// Tests RemoveStackReference when item still has references (line 246-247)
    /// Item should NOT be added to _zeroReferred
    /// </summary>
    [TestMethod]
    public void TestRemoveStackReference_ItemStillHasStackReferences()
    {
        var rc = new ReferenceCounter();
        var array = new Array(rc);

        rc.AddStackReference(array, 3); // Add 3 stack references
        Assert.AreEqual(3, rc.Count);

        rc.RemoveStackReference(array); // Remove 1, still has 2
        Assert.AreEqual(2, rc.Count);

        // Array should NOT be in _zeroReferred
        int count = rc.CheckZeroReferred();
        Assert.AreEqual(2, count); // Array still exists with 2 refs
    }

    /// <summary>
    /// Tests CheckZeroReferred reusing existing _cachedComponents (line 142)
    /// </summary>
    [TestMethod]
    public void TestCheckZeroReferred_ReusesCachedComponents()
    {
        var rc = new ReferenceCounter();
        var array = new Array(rc);

        rc.AddStackReference(array);

        // First call builds _cachedComponents
        rc.CheckZeroReferred();

        // Add to _zeroReferred without invalidating cache
        // (RemoveStackReference to 0 adds to _zeroReferred but doesn't invalidate cache)
        rc.RemoveStackReference(array);

        // Second call should reuse _cachedComponents
        int count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Tests CheckZeroReferred with CompoundType containing non-tracked subitems (line 193)
    /// </summary>
    [TestMethod]
    public void TestCheckZeroReferred_CompoundTypeWithNonTrackedSubitems()
    {
        var rc = new ReferenceCounter();
        var array = new Array(rc);

        rc.AddStackReference(array);
        array.Add(1);  // Integer - not tracked
        array.Add(2);  // Integer - not tracked
        array.Add(3);  // Integer - not tracked

        Assert.AreEqual(4, rc.Count);

        rc.RemoveStackReference(array);
        int count = rc.CheckZeroReferred();

        // All should be cleaned up, including non-tracked subitems
        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Tests Buffer cleanup (item.Cleanup() call at line 201)
    /// </summary>
    [TestMethod]
    public void TestCheckZeroReferred_BufferCleanup()
    {
        var rc = new ReferenceCounter();
        var buffer = new Buffer(100);

        rc.AddStackReference(buffer);
        Assert.AreEqual(1, rc.Count);

        rc.RemoveStackReference(buffer);
        int count = rc.CheckZeroReferred();

        // Buffer should be cleaned up (returned to ArrayPool)
        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Tests that subitems in same component are skipped during cleanup (line 192)
    /// </summary>
    [TestMethod]
    public void TestCheckZeroReferred_SubitemsInSameComponent()
    {
        var rc = new ReferenceCounter();
        var a = new Array(rc);
        var b = new Array(rc);

        rc.AddStackReference(a);

        // Create mutual references - they will be in same SCC
        a.Add(b);
        b.Add(a);

        Assert.AreEqual(3, rc.Count);

        rc.RemoveStackReference(a);
        int count = rc.CheckZeroReferred();

        // Both should be cleaned up together
        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Tests OnStack propagation through parent references (line 163)
    /// </summary>
    [TestMethod]
    public void TestCheckZeroReferred_OnStackPropagation()
    {
        var rc = new ReferenceCounter();
        var root = new Array(rc);
        var child1 = new Array(rc);
        var child2 = new Array(rc);

        rc.AddStackReference(root);

        root.Add(child1);
        child1.Add(child2);
        child2.Add(child1); // Cycle between child1 and child2

        // root -> child1 <-> child2
        // child1 and child2 form a cycle, but are reachable from root

        int count = rc.CheckZeroReferred();

        // All should remain because they're reachable from root
        Assert.AreEqual(4, count);
    }

    /// <summary>
    /// Tests multiple SCCs with different reachability
    /// </summary>
    [TestMethod]
    public void TestCheckZeroReferred_MultipleSCCs()
    {
        var rc = new ReferenceCounter();

        // SCC 1: reachable from stack
        var root = new Array(rc);
        var a = new Array(rc);
        rc.AddStackReference(root);
        root.Add(a);
        a.Add(root); // Cycle

        // SCC 2: not reachable
        var b = new Array(rc);
        var c = new Array(rc);
        rc.AddStackReference(b);
        rc.AddStackReference(c);
        b.Add(c);
        c.Add(b); // Cycle

        // Remove stack refs from SCC 2
        rc.RemoveStackReference(b);
        rc.RemoveStackReference(c);

        int count = rc.CheckZeroReferred();

        // SCC 1 should remain: root(1 stack ref) + a(1 obj ref from root) + root(1 obj ref from a) = 3
        // SCC 2 should be cleaned (0)
        Assert.AreEqual(3, count);
    }

    /// <summary>
    /// Tests Map with CompoundType values that need cleanup
    /// </summary>
    [TestMethod]
    public void TestCheckZeroReferred_MapWithCompoundValues()
    {
        var rc = new ReferenceCounter();
        var map = new Map(rc);
        var innerArray = new Array(rc);

        rc.AddStackReference(map);
        map[(ByteString)"arr"] = innerArray;
        innerArray.Add(42);

        // map(1) + key(1) + innerArray(1) + int(1) = 4
        Assert.AreEqual(4, rc.Count);

        rc.RemoveStackReference(map);
        int count = rc.CheckZeroReferred();

        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Tests Struct with nested Struct
    /// </summary>
    [TestMethod]
    public void TestCheckZeroReferred_NestedStructs()
    {
        var rc = new ReferenceCounter();
        var outer = new Struct(rc);
        var inner = new Struct(rc);

        rc.AddStackReference(outer);
        outer.Add(inner);
        inner.Add(100);
        inner.Add(200);

        // outer(1) + inner(1) + 100(1) + 200(1) = 4
        Assert.AreEqual(4, rc.Count);

        rc.RemoveStackReference(outer);
        int count = rc.CheckZeroReferred();

        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Tests that ObjectReferences dictionary is properly initialized
    /// </summary>
    [TestMethod]
    public void TestAddReference_InitializesObjectReferences()
    {
        var rc = new ReferenceCounter();
        var parent = new Array(rc);
        var child = new Array(rc);

        rc.AddStackReference(parent);

        // First AddReference should initialize ObjectReferences
        rc.AddReference(child, parent);

        // Second AddReference to same parent should reuse existing entry
        rc.AddReference(child, parent);

        Assert.AreEqual(3, rc.Count);

        // Cleanup
        rc.RemoveReference(child, parent);
        rc.RemoveReference(child, parent);
        rc.RemoveStackReference(parent);

        int count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Tests interleaved operations - verifies complex sequences work correctly
    /// </summary>
    [TestMethod]
    public void TestInterleavedOperations()
    {
        var rc = new ReferenceCounter();

        // Create arrays with proper lifecycle management
        var a = new Array(rc);
        var b = new Array(rc);
        var c = new Array(rc);

        // Add stack references
        rc.AddStackReference(a);
        rc.AddStackReference(b);

        // a contains c
        a.Add(c);

        // Verify state
        Assert.AreEqual(3, rc.Count); // a(1) + b(1) + c(1)

        // Check doesn't change anything since all are reachable
        rc.CheckZeroReferred();
        Assert.AreEqual(3, rc.Count);

        // Remove a's stack reference - but c is still referenced by a
        rc.RemoveStackReference(a);

        // a and c should be cleaned up since a has no stack ref
        int count = rc.CheckZeroReferred();
        Assert.AreEqual(1, count); // Only b remains

        // Final cleanup
        rc.RemoveStackReference(b);
        count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Tests that Reset() is called on all tracked items
    /// </summary>
    [TestMethod]
    public void TestCheckZeroReferred_ResetsAllItems()
    {
        var rc = new ReferenceCounter();
        var a = new Array(rc);
        var b = new Array(rc);
        var c = new Array(rc);

        rc.AddStackReference(a);
        rc.AddStackReference(b);
        a.Add(c);

        // First check
        rc.CheckZeroReferred();

        // Modify graph
        b.Add(c);

        // Second check - all items should be reset
        rc.CheckZeroReferred();

        // Cleanup
        rc.RemoveStackReference(a);
        rc.RemoveStackReference(b);

        int count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Removes a child referenced by multiple parents; cleanup should only happen after the last referrer is removed.
    /// </summary>
    [TestMethod]
    public void TestRemoveReference_LastParentTriggersCleanup()
    {
        var rc = new ReferenceCounter();
        var parent1 = new Array(rc);
        var parent2 = new Array(rc);
        var child = new Array(rc);

        rc.AddStackReference(parent1);
        rc.AddStackReference(parent2);
        parent1.Add(child);
        parent2.Add(child);

        Assert.AreEqual(4, rc.Count);

        parent1.RemoveAt(0);
        parent2.RemoveAt(0);

        rc.RemoveStackReference(parent1);
        rc.RemoveStackReference(parent2);

        int count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Multiple references from the same parent should keep the child alive until all references are removed.
    /// </summary>
    [TestMethod]
    public void TestMultipleReferencesFromSameParentRequireAllRemovals()
    {
        var rc = new ReferenceCounter();
        var parent = new Array(rc);
        var child = new Array(rc);

        rc.AddStackReference(parent);
        parent.Add(child);
        parent.Add(child);

        Assert.AreEqual(3, rc.Count);

        parent.RemoveAt(0);
        Assert.AreEqual(2, rc.Count);

        // One reference remains from parent to child, so cleanup should not remove it yet.
        rc.CheckZeroReferred();
        Assert.AreEqual(2, rc.Count);

        parent.RemoveAt(0);
        rc.RemoveStackReference(parent);

        int count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Buffers tracked as map values should be cleaned with their parent container.
    /// </summary>
    [TestMethod]
    public void TestBufferUsedAsMapValueCleanup()
    {
        var rc = new ReferenceCounter();
        var map = new Map(rc);
        var key = (ByteString)"key";
        var value = new Buffer(8);

        rc.AddStackReference(map);
        map[key] = value;

        Assert.AreEqual(3, rc.Count);

        map.Remove(key);
        rc.RemoveStackReference(map);

        int count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Slot indexer should replace references atomically, decrementing the old item and incrementing the new one.
    /// </summary>
    [TestMethod]
    public void TestSlotUpdateReplacesReferences()
    {
        var rc = new ReferenceCounter();
        var slot = new Slot(1, rc);
        var first = new Array(rc);
        var second = new Array(rc);

        // Constructor adds a stack reference for Null
        Assert.AreEqual(1, rc.Count);

        slot[0] = first;
        Assert.AreEqual(1, rc.Count);

        slot[0] = second;
        Assert.AreEqual(1, rc.Count);

        int count = rc.CheckZeroReferred();
        Assert.AreEqual(1, count);

        rc.RemoveStackReference(second);
        count = rc.CheckZeroReferred();
        Assert.AreEqual(0, count);
    }

    #endregion
}
