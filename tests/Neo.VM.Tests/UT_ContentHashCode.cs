// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ContentHashCode.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using Neo.VM.Extensions;
using Neo.VM.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Array = Neo.VM.Types.Array;
using Boolean = Neo.VM.Types.Boolean;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.Test;

[TestClass]
public class UT_ContentHashCode
{
    private static ExecutionEngineLimits Limits
        => ExecutionEngineLimits.Default;

    [TestMethod]
    public void Parameterless_CompoundsAndBuffer_Throw()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => new Array { 1 }.GetHashCode());
        Assert.ThrowsExactly<NotSupportedException>(() => new Struct { 1 }.GetHashCode());
        Assert.ThrowsExactly<NotSupportedException>(() => new Map { [1] = 2 }.GetHashCode());
        Assert.ThrowsExactly<NotSupportedException>(() => new Buffer(1).GetHashCode());
        Assert.AreEqual(ExpectedHash(new Array { 1 }, Limits), new Array { 1 }.GetHashCode(Limits));
    }

    [TestMethod]
    public void ByteString_ContentHash_MatchesToHashCode()
    {
        StackItem itemA = "NEO";
        StackItem itemB = "NEO";
        StackItem itemC = "SmartEconomy";

        Assert.AreEqual(ExpectedHash(itemA, Limits), itemA.GetHashCode(Limits));
        Assert.AreEqual(itemA.GetHashCode(ExecutionEngineLimits.Default), itemA.GetHashCode());
        Assert.AreEqual(itemB.GetHashCode(Limits), itemA.GetHashCode(Limits));
        Assert.AreNotEqual(itemC.GetHashCode(Limits), itemA.GetHashCode(Limits));
        Assert.AreNotEqual(0, itemA.GetHashCode(Limits));
    }

    [TestMethod]
    public void Buffer_ContentHash_EqualContentSameCode()
    {
        byte[] one = [1];
        byte[] two = [2];
        var itemA = new Buffer(one);
        var itemB = new Buffer(one);
        var itemC = new Buffer(two);

        Assert.AreEqual(itemB.GetHashCode(Limits), itemA.GetHashCode(Limits));
        Assert.AreNotEqual(itemC.GetHashCode(Limits), itemA.GetHashCode(Limits));
        Assert.AreEqual(ExpectedHash(itemA, Limits), itemA.GetHashCode(Limits));
        Assert.ThrowsExactly<NotSupportedException>(() => itemA.GetHashCode());
    }

    [TestMethod]
    public void ByteArray_ContentHash_EqualContentSameCode()
    {
        byte[] abc = [1, 2, 3];
        byte[] de = [5, 6];
        StackItem itemA = abc;
        StackItem itemB = abc;
        StackItem itemC = de;

        Assert.AreEqual(itemB.GetHashCode(Limits), itemA.GetHashCode(Limits));
        Assert.AreNotEqual(itemC.GetHashCode(Limits), itemA.GetHashCode(Limits));
    }

    [TestMethod]
    public void Boolean_ContentHash_TrueIsOne_FalseIsZero()
    {
        StackItem itemA = true;
        StackItem itemB = true;
        StackItem itemC = false;

        Assert.AreEqual(ExpectedHash(itemA, Limits), itemA.GetHashCode(Limits));
        Assert.AreEqual(ExpectedHash(itemC, Limits), itemC.GetHashCode(Limits));
        Assert.AreEqual(itemB.GetHashCode(Limits), itemA.GetHashCode(Limits));
        Assert.AreNotEqual(itemC.GetHashCode(Limits), itemA.GetHashCode(Limits));
    }

    [TestMethod]
    public void Integer_ContentHash_UsesSpanHash()
    {
        StackItem itemA = 1;
        StackItem itemB = 1;
        StackItem itemC = 123;

        Assert.AreEqual(ExpectedHash(itemA, Limits), itemA.GetHashCode(Limits));
        Assert.AreEqual(itemB.GetHashCode(Limits), itemA.GetHashCode(Limits));
        Assert.AreNotEqual(itemC.GetHashCode(Limits), itemA.GetHashCode(Limits));
    }

    [TestMethod]
    public void Null_ContentHash_MatchesExpected()
    {
        Assert.AreEqual(ExpectedHash(new Null(), Limits), new Null().GetHashCode(Limits));
        Assert.AreEqual(ExpectedHash(StackItem.Null, Limits), StackItem.Null.GetHashCode(Limits));
        Assert.AreEqual(new Null().GetHashCode(ExecutionEngineLimits.Default), new Null().GetHashCode());
    }

    [TestMethod]
    public void Array_ContentHash_EqualContentSameCode()
    {
        var itemA = new Array { true, false, 0 };
        var itemB = new Array { true, false, 0 };
        var itemC = new Array { true, false, 1 };

        Assert.AreEqual(itemB.GetHashCode(Limits), itemA.GetHashCode(Limits));
        Assert.AreNotEqual(itemC.GetHashCode(Limits), itemA.GetHashCode(Limits));
        Assert.AreEqual(ExpectedHash(itemA, Limits), itemA.GetHashCode(Limits));
    }

    [TestMethod]
    public void Struct_ContentHash_EqualContentSameCode()
    {
        var itemA = new Struct { true, false, 0 };
        var itemB = new Struct { true, false, 0 };
        var itemC = new Struct { true, false, 1 };

        Assert.AreEqual(itemB.GetHashCode(Limits), itemA.GetHashCode(Limits));
        Assert.AreNotEqual(itemC.GetHashCode(Limits), itemA.GetHashCode(Limits));
    }

    [TestMethod]
    public void Map_ContentHash_EqualContentSameCode()
    {
        var itemA = new Map { [true] = false, [0] = 1 };
        var itemB = new Map { [true] = false, [0] = 1 };
        var itemC = new Map { [true] = false, [0] = 2 };

        Assert.AreEqual(itemB.GetHashCode(Limits), itemA.GetHashCode(Limits));
        Assert.AreNotEqual(itemC.GetHashCode(Limits), itemA.GetHashCode(Limits));
        Assert.AreEqual(ExpectedHash(itemA, Limits), itemA.GetHashCode(Limits));
    }

    [TestMethod]
    public void Map_ContentHash_SharedChildArray()
    {
        var junk = new Array { true, false, 0 };
        var itemA = new Map { [true] = junk, [0] = junk };
        var itemB = new Map { [true] = junk, [0] = junk };
        var itemC = new Map { [true] = junk, [0] = 2 };

        Assert.AreEqual(itemB.GetHashCode(Limits), itemA.GetHashCode(Limits));
        Assert.AreNotEqual(itemC.GetHashCode(Limits), itemA.GetHashCode(Limits));
    }

    [TestMethod]
    public void InteropInterface_ContentHash_UsesWrappedObject()
    {
        var itemA = new InteropInterface(123);
        var itemB = new InteropInterface(123);
        var itemC = new InteropInterface(124);

        Assert.AreEqual(itemB.GetHashCode(Limits), itemA.GetHashCode(Limits));
        Assert.AreNotEqual(itemC.GetHashCode(Limits), itemA.GetHashCode(Limits));
        Assert.AreEqual(itemA.GetHashCode(), itemA.GetHashCode(Limits));
    }

    [TestMethod]
    public void Pointer_ContentHash_UsesPositionAndScriptBytes()
    {
        var script = new Script(System.Array.Empty<byte>());
        var itemA = new Pointer(script, 123);
        var itemB = new Pointer(script, 123);
        var itemC = new Pointer(script, 1234);

        Assert.AreEqual(itemB.GetHashCode(Limits), itemA.GetHashCode(Limits));
        Assert.AreNotEqual(itemC.GetHashCode(Limits), itemA.GetHashCode(Limits));
        var span = itemA.GetSpan(Limits);
        var expected = HashCode.Combine(itemA.Type, span.Length, itemA.Position, span.ToHashCode(397));
        Assert.AreEqual(expected, itemA.GetHashCode(Limits));
        Assert.AreEqual(itemA.GetHashCode(ExecutionEngineLimits.Default), itemA.GetHashCode());
    }

    [TestMethod]
    public void ToHashCode_Helpers()
    {
        byte[] data = [1, 2, 3];
        Assert.AreEqual(data.AsSpan().ToHashCode(397), data.ToHashCode());
        Assert.AreEqual(data.AsSpan().ToHashCode(7), data.AsMemory().ToHashCode(7));
        Assert.AreEqual(0, System.Array.Empty<byte>().AsSpan().ToHashCode(0));

        Memory<byte> memory = data;
        Assert.AreEqual(data.AsSpan().ToHashCode(data.Length), memory.ToHashCode());
        ReadOnlyMemory<byte> rom = data;
        Assert.AreEqual(data.AsSpan().ToHashCode(data.Length), rom.ToHashCode());
        Assert.AreEqual(data.AsSpan().ToHashCode(9), rom.ToHashCode(9));

        int[] nums = [1, 2];
        var expectedNums = 397;
        expectedNums = unchecked((expectedNums * 31) ^ 1);
        expectedNums = unchecked((expectedNums * 31) ^ 2);
        Assert.AreEqual(expectedNums, EnumerableExtensions.ToHashCode(nums));

        IList<int> list = new List<int> { 1, 2 };
        Assert.AreEqual(expectedNums, list.ToHashCode());
        IReadOnlyList<int> readOnlyList = new List<int> { 1, 2 };
        Assert.AreEqual(expectedNums, ListExtensions.ToHashCode(readOnlyList));

        List<string> withNull = [null, "a"];
        var expectedNulls = 397;
        expectedNulls = unchecked((expectedNulls * 31) ^ 0);
        expectedNulls = unchecked((expectedNulls * 31) ^ "a".GetHashCode());
        Assert.AreEqual(expectedNulls, EnumerableExtensions.ToHashCode(withNull));

        var dict = new Dictionary<int, int> { [1] = 2 };
        var expectedDict = unchecked((397 * 31) + (1.GetHashCode() ^ 2.GetHashCode()));
        Assert.AreEqual(expectedDict, DictionaryExtensions.ToHashCode((IDictionary<int, int>)dict));
        IReadOnlyDictionary<int, int> readOnlyDict = dict;
        Assert.AreEqual(expectedDict, DictionaryExtensions.ToHashCode(readOnlyDict));
    }

    [TestMethod]
    public void ToStackItem_PrimitivesAndCollections()
    {
        object missing = null;
        Assert.AreSame(StackItem.Null, missing.ToStackItem());

        StackItem existing = 1;
        Assert.AreSame(existing, existing.ToStackItem());

        Assert.IsInstanceOfType<Integer>(1.ToStackItem());
        Assert.AreEqual(1, 1.ToStackItem().GetInteger());
        Assert.AreEqual(1, ((byte)1).ToStackItem().GetInteger());
        Assert.AreEqual(-2, ((sbyte)(-2)).ToStackItem().GetInteger());
        Assert.AreEqual(-3, ((short)(-3)).ToStackItem().GetInteger());
        Assert.AreEqual(4, ((ushort)4).ToStackItem().GetInteger());
        Assert.AreEqual(5u, ((uint)5).ToStackItem().GetInteger());
        Assert.AreEqual(6L, ((long)6).ToStackItem().GetInteger());
        Assert.AreEqual(7UL, ((ulong)7).ToStackItem().GetInteger());
        Assert.AreEqual(new BigInteger(8), ((BigInteger)8).ToStackItem().GetInteger());

        Assert.IsInstanceOfType<Boolean>(true.ToStackItem());
        Assert.IsInstanceOfType<ByteString>("NEO".ToStackItem());
        byte[] data = [1, 2];
        Assert.IsInstanceOfType<ByteString>(data.ToStackItem());
        Memory<byte> memory = data;
        CollectionAssert.AreEqual(data, ((ByteString)memory.ToStackItem()).GetSpan().ToArray());
        ReadOnlyMemory<byte> rom = data;
        CollectionAssert.AreEqual(data, ((ByteString)rom.ToStackItem()).GetSpan().ToArray());
        Assert.IsInstanceOfType<Array>(new[] { 1, 2 }.ToStackItem());

        var entries = new Dictionary<int, string> { [1] = "a" };
        var map = (Map)entries.ToStackItem();
        Assert.AreEqual(1, map.Count);
        Assert.AreEqual("a", map[1].GetString());

        var withNullValue = new Hashtable { [1] = null };
        var nullMap = (Map)withNullValue.ToStackItem();
        Assert.IsTrue(nullMap[1].IsNull);

        var badKey = new Hashtable { [new object()] = 2 };
        Assert.ThrowsExactly<InvalidCastException>(() => badKey.ToStackItem());

        Assert.IsInstanceOfType<InteropInterface>(new object().ToStackItem());
    }

    [TestMethod]
    public void GetHashCode_Primitives_MatchLimits()
    {
        StackItem number = 7;
        Assert.AreEqual(number.GetHashCode(ExecutionEngineLimits.Default), number.GetHashCode());
        Assert.AreEqual(ExpectedHash(number, ExecutionEngineLimits.Default), number.GetHashCode());

        StackItem flag = true;
        Assert.AreEqual(ExpectedHash(flag, ExecutionEngineLimits.Default), flag.GetHashCode());

        Assert.AreEqual(ExpectedHash(StackItem.Null, ExecutionEngineLimits.Default), StackItem.Null.GetHashCode());
        Assert.AreEqual(new Null().GetHashCode(ExecutionEngineLimits.Default), new Null().GetHashCode());

        var script = new Script(System.Array.Empty<byte>());
        var pointer = new Pointer(script, 1);
        var pointerSpan = pointer.GetSpan();
        Assert.AreEqual(HashCode.Combine(pointer.Type, pointerSpan.Length, pointer.Position, pointerSpan.ToHashCode(397)), pointer.GetHashCode());
        Assert.IsTrue(pointer.Equals(new Pointer(script, 1)));
        Assert.IsFalse(pointer.Equals((StackItem)1));
        Assert.IsFalse(((StackItem)true).Equals((StackItem)1));
    }

    [TestMethod]
    public void GetHashCode_ContentHash_EveryType()
    {
        Assert.AreEqual(((StackItem)false).GetHashCode(Limits), ((StackItem)false).GetHashCode(Limits));
        Assert.AreEqual(ExpectedHash((Integer)0, Limits), ((Integer)0).GetHashCode(Limits));

        ByteString empty = System.Array.Empty<byte>();
        Assert.AreEqual(ExpectedHash(empty, Limits), empty.GetHashCode(Limits));

        var emptyArray = new Array();
        Assert.AreEqual(ExpectedHash(emptyArray, Limits), emptyArray.GetHashCode(Limits));
        var emptyMap = new Map();
        Assert.AreEqual(ExpectedHash(emptyMap, Limits), emptyMap.GetHashCode(Limits));
        var emptyStruct = new Struct();
        Assert.AreEqual(ExpectedHash(emptyStruct, Limits), emptyStruct.GetHashCode(Limits));
    }

    private static int ExpectedHash(StackItem item, ExecutionEngineLimits limits)
    {
        var span = item is CompoundType ? item.GetSafeSpan() : item.GetSpan(limits);
        return HashCode.Combine(item.Type, span.Length, span.ToHashCode(397));
    }
}
