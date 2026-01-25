// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Optimizations.cs file belongs to the neo project and is free
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
using VMArray = Neo.VM.Types.Array;
using VMBuffer = Neo.VM.Types.Buffer;

namespace Neo.Test;

/// <summary>
/// Unit tests for Neo.VM performance optimizations.
/// Tests the optimized constructors, bulk operations, and HasTrackableSubItems property.
/// </summary>
[TestClass]
public class UT_Optimizations
{
    [TestMethod]
    public void Array_OptimizedConstructor_CreatesCorrectCount()
    {
        var rc = new ReferenceCounter();
        var count = 16;

        var arr = new VMArray(rc, StackItem.Null, count, skipReferenceCounting: true);

        Assert.AreEqual(count, arr.Count);
        Assert.AreEqual(StackItemType.Array, arr.Type);
    }

    [TestMethod]
    public void Array_OptimizedConstructor_AllElementsAreNull()
    {
        var rc = new ReferenceCounter();
        var count = 10;

        var arr = new VMArray(rc, StackItem.Null, count, skipReferenceCounting: true);

        for (int i = 0; i < count; i++)
        {
            Assert.IsTrue(arr[i].IsNull);
        }
    }

    [TestMethod]
    public void Array_OptimizedConstructor_ZeroCount_CreatesEmptyArray()
    {
        var rc = new ReferenceCounter();

        var arr = new VMArray(rc, StackItem.Null, 0, skipReferenceCounting: true);

        Assert.AreEqual(0, arr.Count);
    }

    [TestMethod]
    public void Array_OptimizedConstructor_NegativeCount_ThrowsException()
    {
        var rc = new ReferenceCounter();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            new VMArray(rc, StackItem.Null, -1, skipReferenceCounting: true);
        });
    }

    [TestMethod]
    public void Array_OptimizedConstructor_RejectsCompoundWithoutReferenceCounter()
    {
        var rc = new ReferenceCounter();
        var item = new Struct();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            new VMArray(rc, item, 1, skipReferenceCounting: false));
    }

    [TestMethod]
    public void Array_HasTrackableSubItems_EmptyArray_ReturnsFalse()
    {
        var rc = new ReferenceCounter();
        var arr = new VMArray(rc);

        Assert.IsFalse(arr.HasTrackableSubItems);
    }

    [TestMethod]
    public void Array_HasTrackableSubItems_WithIntegers_ReturnsFalse()
    {
        var rc = new ReferenceCounter();
        var arr = new VMArray(rc);
        for (int i = 0; i < 10; i++)
        {
            arr.Add(new Integer(i));
        }

        Assert.IsFalse(arr.HasTrackableSubItems);
    }

    [TestMethod]
    public void Array_HasTrackableSubItems_WithBuffer_ReturnsTrue()
    {
        var rc = new ReferenceCounter();
        var arr = new VMArray(rc)
        {
            new VMBuffer(16)
        };

        Assert.IsTrue(arr.HasTrackableSubItems);
    }

    [TestMethod]
    public void Array_HasTrackableSubItems_WithNestedArray_ReturnsTrue()
    {
        var rc = new ReferenceCounter();
        var arr = new VMArray(rc)
        {
            new VMArray(rc)
        };

        Assert.IsTrue(arr.HasTrackableSubItems);
    }

    [TestMethod]
    public void Array_HasTrackableSubItems_AfterRemovingTrackableItem_ReturnsFalse()
    {
        var rc = new ReferenceCounter();
        var arr = new VMArray(rc)
        {
            Integer.Zero,
            new VMBuffer(16)
        };

        Assert.IsTrue(arr.HasTrackableSubItems);

        arr.RemoveAt(1);

        Assert.IsFalse(arr.HasTrackableSubItems);
    }

    [TestMethod]
    public void Array_HasTrackableSubItems_AfterAddingTrackableItem_ReturnsTrue()
    {
        var rc = new ReferenceCounter();
        var arr = new VMArray(rc)
        {
            Integer.Zero
        };
        Assert.IsFalse(arr.HasTrackableSubItems);

        arr.Add(new VMBuffer(16));
        Assert.IsTrue(arr.HasTrackableSubItems);
    }

    [TestMethod]
    public void ReferenceCounter_BulkAddReference_AddsCorrectCount()
    {
        var rc = new ReferenceCounter();
        var item = new VMBuffer(16);
        var parent = new VMArray(rc);
        var count = 10;

        rc.AddReference(item, parent, count);

        Assert.IsTrue(item.ObjectReferences.ContainsKey(parent));
        Assert.AreEqual(count, item.ObjectReferences[parent].References);
        Assert.AreEqual(count, rc.Count);
    }

    [TestMethod]
    public void ReferenceCounter_BulkAddReference_ZeroCount_DoesNothing()
    {
        var rc = new ReferenceCounter();
        var item = new VMBuffer(16);
        var parent = new VMArray(rc);

        rc.AddReference(item, parent, 0);

        // Should not throw, but may not add references either
        Assert.AreEqual(0, rc.Count);
    }

    [TestMethod]
    public void ReferenceCounter_BulkAddReference_AfterSingleAdd_Increments()
    {
        var rc = new ReferenceCounter();
        var item = new VMBuffer(16);
        var parent = new VMArray(rc);

        // Add single reference
        rc.AddReference(item, parent);
        Assert.AreEqual(1, item.ObjectReferences[parent].References);

        // Add bulk references
        rc.AddReference(item, parent, 5);
        Assert.AreEqual(6, item.ObjectReferences[parent].References);
        Assert.AreEqual(6, rc.Count);
    }

    [TestMethod]
    public void ReferenceCounter_BulkVsSingleAdd_ProduceSameResult()
    {
        var rc1 = new ReferenceCounter();
        var rc2 = new ReferenceCounter();
        var item1 = new VMBuffer(16);
        var item2 = new VMBuffer(16);
        var parent1 = new VMArray(rc1);
        var parent2 = new VMArray(rc2);
        var count = 100;

        // Using bulk add
        rc1.AddReference(item1, parent1, count);

        // Using single adds in loop
        for (int i = 0; i < count; i++)
        {
            rc2.AddReference(item2, parent2);
        }

        // Both should have the same reference count
        Assert.AreEqual(rc1.Count, rc2.Count);
        Assert.AreEqual(count, rc1.Count);
        Assert.AreEqual(count, rc2.Count);
    }

    [TestMethod]
    public void ReferenceCounter_BulkAddReference_MultipleParents_TracksSeparately()
    {
        var rc = new ReferenceCounter();
        var item = new VMBuffer(16);
        var parent1 = new VMArray(rc);
        var parent2 = new VMArray(rc);

        rc.AddReference(item, parent1, 5);
        rc.AddReference(item, parent2, 3);

        Assert.IsTrue(item.ObjectReferences.ContainsKey(parent1));
        Assert.IsTrue(item.ObjectReferences.ContainsKey(parent2));
        Assert.AreEqual(5, item.ObjectReferences[parent1].References);
        Assert.AreEqual(3, item.ObjectReferences[parent2].References);
        Assert.AreEqual(8, rc.Count);
    }

    [TestMethod]
    public void Slot_Indexer_ValidIndex_ReturnsCorrectItem()
    {
        var rc = new ReferenceCounter();
        var items = new StackItem[3];
        items[0] = new Integer(1);
        items[1] = new Integer(2);
        items[2] = new Integer(3);
        var slot = new Slot(items, rc);

        Assert.AreEqual(new Integer(1), slot[0]);
        Assert.AreEqual(new Integer(2), slot[1]);
        Assert.AreEqual(new Integer(3), slot[2]);
    }

    [TestMethod]
    public void Slot_Indexer_InvalidLowerBound_ThrowsArgumentOutOfRangeException()
    {
        var rc = new ReferenceCounter();
        var slot = new Slot(3, rc);

        Assert.ThrowsExactly<IndexOutOfRangeException>(() =>
        {
            _ = slot[-1];
        });
    }

    [TestMethod]
    public void Slot_Indexer_InvalidUpperBound_ThrowsArgumentOutOfRangeException()
    {
        var rc = new ReferenceCounter();
        var slot = new Slot(3, rc);

        Assert.ThrowsExactly<IndexOutOfRangeException>(() =>
        {
            _ = slot[3];
        });
    }

    [TestMethod]
    public void Struct_HasTrackableSubItems_WorksLikeArray()
    {
        var rc = new ReferenceCounter();
        var structItem = new Struct(rc);

        Assert.IsFalse(structItem.HasTrackableSubItems);

        structItem.Add(new VMBuffer(16));

        Assert.IsTrue(structItem.HasTrackableSubItems);
    }

    [TestMethod]
    public void Array_OptimizedVsStandardConstructor_ProduceSameResult()
    {
        var rc = new ReferenceCounter();
        var count = 10;
        var item = Integer.Zero;

        // Create using optimized constructor
        var arr1 = new VMArray(rc, item, count, skipReferenceCounting: true);

        // Create using standard constructor
        var tempArray = new StackItem[count];
        for (int i = 0; i < count; i++)
        {
            tempArray[i] = item;
        }
        var arr2 = new VMArray(rc, tempArray);

        // Both should have the same count
        Assert.AreEqual(arr1.Count, arr2.Count);

        // Both should have the same elements
        for (int i = 0; i < count; i++)
        {
            Assert.AreEqual(arr1[i].GetType(), arr2[i].GetType());
        }
    }

}
