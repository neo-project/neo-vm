// Copyright (C) 2015-2026 The Neo Project.
//
// UT_SafeSpan.cs file belongs to the neo project and is free
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
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.Test;

[TestClass]
public class UT_SafeSpan
{
    [TestMethod]
    public void Primitive_GetSafeSpan_MatchesGetSpan()
    {
        StackItem integer = 1;
        CollectionAssert.AreEqual(integer.GetSpan().ToArray(), integer.GetSafeSpan().ToArray());

        StackItem flag = true;
        CollectionAssert.AreEqual(flag.GetSpan().ToArray(), flag.GetSafeSpan().ToArray());

        byte[] data = [1, 2, 3];
        StackItem bytes = data;
        CollectionAssert.AreEqual(data, bytes.GetSafeSpan().ToArray());

        var buffer = new Buffer(data);
        CollectionAssert.AreEqual(data, buffer.GetSafeSpan().ToArray());
    }

    [TestMethod]
    public void Array_ConcatenatesChildSpans()
    {
        var array = new Array { 1, 2 };
        byte[] expected = [1, 2];
        CollectionAssert.AreEqual(expected, array.GetSafeSpan().ToArray());
    }

    [TestMethod]
    public void Array_SkipsNullItems()
    {
        var array = new Array { 1, StackItem.Null, 2 };
        byte[] expected = [1, 2];
        CollectionAssert.AreEqual(expected, array.GetSafeSpan().ToArray());
    }

    [TestMethod]
    public void Array_CircularReference_DoesNotRecurse()
    {
        Array array = [1];
        array.Add(array);
        byte[] expected = [1];
        CollectionAssert.AreEqual(expected, array.GetSafeSpan().ToArray());
    }

    [TestMethod]
    public void Struct_CircularReference_DoesNotRecurse()
    {
        Struct s = [];
        s.Add(s);
        Assert.AreEqual(0, s.GetSafeSpan().Length);
    }

    [TestMethod]
    public void Map_ConcatenatesKeyAndValueSpans()
    {
        var map = new Map { [1] = 2 };
        byte[] expected = [1, 2];
        CollectionAssert.AreEqual(expected, map.GetSafeSpan().ToArray());
    }

    [TestMethod]
    public void Null_GetSafeSpan_IsEmpty()
    {
        Assert.AreEqual(0, StackItem.Null.GetSafeSpan().Length);
    }

    [TestMethod]
    public void GetSpan_UsesGetSafeSpan_ForPrimitives()
    {
        StackItem number = 7;
        CollectionAssert.AreEqual(number.GetSafeSpan().ToArray(), number.GetSpan().ToArray());
        CollectionAssert.AreEqual(
            number.GetSpan(ExecutionEngineLimits.Default).ToArray(),
            number.GetSpan().ToArray());

        byte[] data = [1, 2, 3];
        var buffer = new Buffer(data);
        CollectionAssert.AreEqual(data, buffer.GetSpan().ToArray());
        CollectionAssert.AreEqual(buffer.GetSafeSpan().ToArray(), buffer.GetSpan().ToArray());
        CollectionAssert.AreEqual(
            buffer.GetSpan(ExecutionEngineLimits.Default).ToArray(),
            buffer.GetSpan().ToArray());
    }

    [TestMethod]
    public void GetSpan_OnCompound_Throws()
    {
        var array = new Array { 1, 2 };
        Assert.ThrowsExactly<InvalidCastException>(() => array.GetSpan());
        Assert.ThrowsExactly<InvalidCastException>(() =>
            array.GetSpan(ExecutionEngineLimits.Default));

        byte[] expected = [1, 2];
        CollectionAssert.AreEqual(expected, array.GetSafeSpan().ToArray());

        var map = new Map { [1] = 2 };
        Assert.ThrowsExactly<InvalidCastException>(() => map.GetSpan());
        CollectionAssert.AreEqual(expected, map.GetSafeSpan().ToArray());
    }

    [TestMethod]
    public void GetSpan_OnCompound_ExceedsMaxItemSize()
    {
        var data = new byte[(int)ExecutionEngineLimits.Default.MaxItemSize];
        var array = new Array { new ByteString(data), 1 };
        Assert.ThrowsExactly<InvalidCastException>(() => array.GetSpan());
        Assert.ThrowsExactly<InvalidCastException>(() =>
            array.GetSpan(ExecutionEngineLimits.Default));
    }

    [TestMethod]
    public void Size_MatchesAsSpanLength()
    {
        StackItem number = 7;
        Assert.AreEqual(number.GetSpan().Length, number.Size);

        var array = new Array { 1, 2 };
        Assert.ThrowsExactly<InvalidCastException>(() => _ = array.Size);

        byte[] data = [1, 2, 3];
        var buffer = new Buffer(data);
        Assert.AreEqual(data.Length, buffer.Size);
        Assert.AreEqual(buffer.GetSpan().Length, buffer.Size);
    }

    [TestMethod]
    public void GetSafeSpan_MatchesGetSpan_ForPrimitives()
    {
        StackItem item = 7;
        CollectionAssert.AreEqual(item.GetSpan().ToArray(), item.GetSafeSpan().ToArray());

        var array = new Array { 1, 2 };
        Assert.ThrowsExactly<InvalidCastException>(() => array.GetSpan());
        CollectionAssert.AreEqual(array.GetSafeSpan().ToArray(), new byte[] { 1, 2 });
    }

    [TestMethod]
    public void ExplicitConversions_FromStackItem()
    {
        StackItem number = 42;
        Assert.AreEqual(42, (int)number);
        Assert.AreEqual(42L, (long)number);
        Assert.AreEqual(new BigInteger(42), (BigInteger)number);
        Assert.AreEqual((byte)42, (byte)number);
        Assert.AreEqual((sbyte)42, (sbyte)number);
        Assert.AreEqual((short)42, (short)number);
        Assert.AreEqual((ushort)42, (ushort)number);
        Assert.AreEqual(42u, (uint)number);
        Assert.AreEqual(42UL, (ulong)number);

        StackItem flag = true;
        Assert.IsTrue((bool)flag);

        byte[] data = [1, 2, 3];
        StackItem bytes = data;
        CollectionAssert.AreEqual(data, (byte[])bytes);

        Assert.AreEqual("42", (string)number);
    }

    [TestMethod]
    public void ExplicitConversions_IntegerBooleanBuffer()
    {
        Integer n = 5;
        Assert.AreEqual(5, (int)n);
        Assert.AreEqual(new BigInteger(5), (BigInteger)n);
        Assert.IsTrue((bool)n);

        Neo.VM.Types.Boolean flag = true;
        Assert.IsTrue((bool)flag);
        Assert.AreEqual(BigInteger.One, (BigInteger)flag);

        byte[] data = [1, 2];
        Buffer buffer = data;
        CollectionAssert.AreEqual(data, (byte[])buffer);
        Assert.AreEqual(new BigInteger(data), (BigInteger)buffer);

        ByteString bs = data;
        CollectionAssert.AreEqual(data, (byte[])bs);
    }

    [TestMethod]
    public void ExplicitBigInteger_UsesSignedGetInteger()
    {
        Integer n = 5;
        Assert.AreEqual(new BigInteger(5), (BigInteger)n);
        Assert.AreEqual((BigInteger)(Integer)(-1), new BigInteger(-1));

        byte[] highBit = [0x80];
        Buffer hb = highBit;
        var signed = new BigInteger(highBit);
        Assert.AreEqual(signed, (BigInteger)hb);

        StackItem fromBytes = highBit;
        Assert.AreEqual(new BigInteger(highBit), (BigInteger)fromBytes);
    }

    [TestMethod]
    public void EmptyAndNested_GetSafeSpan()
    {
        Array emptyArray = [];
        Map emptyMap = [];
        Assert.AreEqual(0, emptyArray.GetSafeSpan().Length);
        Assert.AreEqual(0, emptyMap.GetSafeSpan().Length);
        Assert.AreEqual(0, ((StackItem)0).GetSafeSpan().Length);
        CollectionAssert.AreEqual(new byte[] { 0 }, ((StackItem)false).GetSafeSpan().ToArray());

        var nested = new Array { new Array { 1, 2 }, 3 };
        byte[] expected = [1, 2, 3];
        CollectionAssert.AreEqual(expected, nested.GetSafeSpan().ToArray());
    }

    [TestMethod]
    public void Map_CircularReference_DoesNotRecurse()
    {
        Map map = [];
        map[1] = map;
        byte[] expected = [1];
        CollectionAssert.AreEqual(expected, map.GetSafeSpan().ToArray());
    }

    [TestMethod]
    public void PointerAndInterop_GetSafeSpan_IsEmpty()
    {
        var pointer = new Pointer(Script.Empty, 0);
        Assert.AreEqual(0, pointer.GetSafeSpan().Length);
        var interop = new InteropInterface(new object());
        Assert.AreEqual(0, interop.GetSafeSpan().Length);
    }

    [TestMethod]
    public void Array_ContainsEveryVmType()
    {
        var items = AllVmTypes();
        var array = new Array(items);
        var expected = ConcatSkippingNull(items);
        CollectionAssert.AreEqual(expected, array.GetSafeSpan().ToArray());
        Assert.ThrowsExactly<InvalidCastException>(() => array.GetSpan());
        Assert.ThrowsExactly<InvalidCastException>(() => array.GetSpan(ExecutionEngineLimits.Default));
    }

    [TestMethod]
    public void Struct_ContainsEveryVmType()
    {
        var items = AllVmTypes();
        var s = new Struct(items);
        var expected = ConcatSkippingNull(items);
        CollectionAssert.AreEqual(expected, s.GetSafeSpan().ToArray());
        Assert.ThrowsExactly<InvalidCastException>(() => s.GetSpan());
        Assert.ThrowsExactly<InvalidCastException>(() => s.GetSpan(ExecutionEngineLimits.Default));
    }

    [TestMethod]
    public void Map_ContainsEveryVmTypeAsValue()
    {
        var items = AllVmTypes();
        var map = new Map();
        for (var i = 0; i < items.Length; i++)
            map[i + 1] = items[i];

        var expected = ConcatMap(map);
        CollectionAssert.AreEqual(expected, map.GetSafeSpan().ToArray());
        Assert.ThrowsExactly<InvalidCastException>(() => map.GetSpan());
        Assert.ThrowsExactly<InvalidCastException>(() => map.GetSpan(ExecutionEngineLimits.Default));
    }

    [TestMethod]
    public void Array_EachVmType_Alone()
    {
        foreach (var item in AllVmTypes())
        {
            var array = new Array { item };
            var expected = item is Null ? [] : item.GetSafeSpan().ToArray();
            CollectionAssert.AreEqual(expected, array.GetSafeSpan().ToArray(), $"Array[{item.GetType().Name}]");
            Assert.ThrowsExactly<InvalidCastException>(() => array.GetSpan(), $"Array.GetSpan[{item.GetType().Name}]");
        }
    }

    [TestMethod]
    public void Struct_EachVmType_Alone()
    {
        foreach (var item in AllVmTypes())
        {
            var s = new Struct { item };
            var expected = item is Null ? [] : item.GetSafeSpan().ToArray();
            CollectionAssert.AreEqual(expected, s.GetSafeSpan().ToArray(), $"Struct[{item.GetType().Name}]");
            Assert.ThrowsExactly<InvalidCastException>(() => s.GetSpan(), $"Struct.GetSpan[{item.GetType().Name}]");
        }
    }

    [TestMethod]
    public void Map_EachVmType_Alone()
    {
        var key = 1;
        foreach (var item in AllVmTypes())
        {
            var map = new Map { [key] = item };
            var expected = ConcatMap(map);
            CollectionAssert.AreEqual(expected, map.GetSafeSpan().ToArray(), $"Map[{item.GetType().Name}]");
            Assert.ThrowsExactly<InvalidCastException>(() => map.GetSpan(), $"Map.GetSpan[{item.GetType().Name}]");
        }
    }

    [TestMethod]
    public void Map_PrimitiveKeys_IntegerBooleanByteString()
    {
        byte[] payload = [9, 8];
        var map = new Map
        {
            [1] = payload,
            [true] = 2,
            [false] = 3,
            [(ByteString)payload] = true
        };

        var expected = ConcatMap(map);
        CollectionAssert.AreEqual(expected, map.GetSafeSpan().ToArray());
        Assert.ThrowsExactly<InvalidCastException>(() => map.GetSpan());
    }

    [TestMethod]
    public void NestedCompounds_ContainEveryVmType()
    {
        var items = AllVmTypes();
        var innerArray = new Array(items);
        var innerStruct = new Struct(items);
        var innerMap = new Map();
        for (var i = 0; i < items.Length; i++)
            innerMap[i + 1] = items[i];

        StackItem[] nested = [innerArray, innerStruct, innerMap];
        var array = new Array(nested);
        CollectionAssert.AreEqual(ConcatSkippingNull(nested), array.GetSafeSpan().ToArray());

        var s = new Struct(nested);
        CollectionAssert.AreEqual(ConcatSkippingNull(nested), s.GetSafeSpan().ToArray());

        var map = new Map
        {
            [1] = innerArray,
            [2] = innerStruct,
            [3] = innerMap
        };
        CollectionAssert.AreEqual(ConcatMap(map), map.GetSafeSpan().ToArray());
    }

    private static StackItem[] AllVmTypes()
    {
        byte[] data = [4, 5];
        return
        [
            StackItem.Null,
            true,
            false,
            (Integer)0,
            (Integer)1,
            (ByteString)data,
            new Buffer(data),
            new Array { 9 },
            new Struct { 8 },
            new Map { [7] = 6 },
            new Pointer(Script.Empty, 0),
            new InteropInterface(new object())
        ];
    }

    private static byte[] ConcatSkippingNull(IEnumerable<StackItem> items)
    {
        var result = new List<byte>();
        foreach (var item in items)
        {
            if (item is Null)
                continue;
            result.AddRange(item.GetSafeSpan().ToArray());
        }
        return result.ToArray();
    }

    private static byte[] ConcatMap(Map map)
    {
        var result = new List<byte>();
        foreach (var (key, value) in map)
        {
            result.AddRange(key.GetSafeSpan().ToArray());
            result.AddRange(value.GetSafeSpan().ToArray());
        }
        return result.ToArray();
    }
}
