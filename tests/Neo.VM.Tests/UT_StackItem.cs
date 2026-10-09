// Copyright (C) 2015-2026 The Neo Project.
//
// UT_StackItem.cs file belongs to the neo project and is free
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
using System.Numerics;
using Array = Neo.VM.Types.Array;
using Boolean = Neo.VM.Types.Boolean;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.Test;

[TestClass]
public class UT_StackItem
{
    [TestMethod]
    public void TestCircularReference()
    {
        var itemA = new Struct { true, false };
        var itemB = new Struct { true, false };
        var itemC = new Struct { false, false };

        itemA[1] = itemA;
        itemB[1] = itemB;
        itemC[1] = itemC;

        Assert.ThrowsExactly<InvalidOperationException>(() => itemA.Equals(itemB, ExecutionEngineLimits.Default));
        Assert.IsTrue(itemA.Equals(itemB));
        Assert.IsFalse(itemA.Equals(itemC));
    }

    [TestMethod]
    public void TestHashCode()
    {
        StackItem itemA = "NEO";
        StackItem itemB = "NEO";
        StackItem itemC = "SmartEconomy";

        Assert.AreEqual(itemB.GetHashCode(), itemA.GetHashCode());
        Assert.AreNotEqual(itemC.GetHashCode(), itemA.GetHashCode());

        itemA = new Buffer(1);
        itemB = new Buffer(1);
        itemC = new Buffer(2);

        Assert.ThrowsExactly<NotSupportedException>(() => itemA.GetHashCode());
        Assert.ThrowsExactly<NotSupportedException>(() => itemB.GetHashCode());
        Assert.ThrowsExactly<NotSupportedException>(() => itemC.GetHashCode());

        itemA = new byte[] { 1, 2, 3 };
        itemB = new byte[] { 1, 2, 3 };
        itemC = new byte[] { 5, 6 };

        Assert.AreEqual(itemB.GetHashCode(), itemA.GetHashCode());
        Assert.AreNotEqual(itemC.GetHashCode(), itemA.GetHashCode());

        itemA = true;
        itemB = true;
        itemC = false;

        Assert.AreEqual(itemB.GetHashCode(), itemA.GetHashCode());
        Assert.AreNotEqual(itemC.GetHashCode(), itemA.GetHashCode());

        itemA = 1;
        itemB = 1;
        itemC = 123;

        Assert.AreEqual(itemB.GetHashCode(), itemA.GetHashCode());
        Assert.AreNotEqual(itemC.GetHashCode(), itemA.GetHashCode());

        itemA = new Null();
        itemB = new Null();

        Assert.AreEqual(itemB.GetHashCode(), itemA.GetHashCode());

        itemA = new Array { true, false, 0 };
        itemB = new Array { true, false, 0 };
        itemC = new Array { true, false, 1 };

        Assert.ThrowsExactly<NotSupportedException>(() => itemA.GetHashCode());
        Assert.ThrowsExactly<NotSupportedException>(() => itemB.GetHashCode());
        Assert.ThrowsExactly<NotSupportedException>(() => itemC.GetHashCode());

        itemA = new Struct { true, false, 0 };
        itemB = new Struct { true, false, 0 };
        itemC = new Struct { true, false, 1 };

        Assert.ThrowsExactly<NotSupportedException>(() => itemA.GetHashCode());
        Assert.ThrowsExactly<NotSupportedException>(() => itemB.GetHashCode());
        Assert.ThrowsExactly<NotSupportedException>(() => itemC.GetHashCode());

        itemA = new Map { [true] = false, [0] = 1 };
        itemB = new Map { [true] = false, [0] = 1 };
        itemC = new Map { [true] = false, [0] = 2 };

        Assert.ThrowsExactly<NotSupportedException>(() => itemA.GetHashCode());
        Assert.ThrowsExactly<NotSupportedException>(() => itemB.GetHashCode());
        Assert.ThrowsExactly<NotSupportedException>(() => itemC.GetHashCode());

        // Test CompoundType GetHashCode for subitems
        var junk = new Array { true, false, 0 };
        itemA = new Map { [true] = junk, [0] = junk };
        itemB = new Map { [true] = junk, [0] = junk };
        itemC = new Map { [true] = junk, [0] = 2 };

        Assert.ThrowsExactly<NotSupportedException>(() => itemA.GetHashCode());
        Assert.ThrowsExactly<NotSupportedException>(() => itemB.GetHashCode());
        Assert.ThrowsExactly<NotSupportedException>(() => itemC.GetHashCode());

        itemA = new InteropInterface(123);
        itemB = new InteropInterface(123);
        itemC = new InteropInterface(124);

        Assert.AreEqual(itemB.GetHashCode(), itemA.GetHashCode());
        Assert.AreNotEqual(itemC.GetHashCode(), itemA.GetHashCode());

        byte[] emptyScript = [];
        var script = new Script(emptyScript);
        itemA = new Pointer(script, 123);
        itemB = new Pointer(script, 123);
        itemC = new Pointer(script, 1234);

        Assert.AreEqual(itemB.GetHashCode(), itemA.GetHashCode());
        Assert.AreNotEqual(itemC.GetHashCode(), itemA.GetHashCode());
    }

    [TestMethod]
    public void TestNull()
    {
        byte[] empty = [];
        StackItem nullItem = empty;
        Assert.AreNotEqual(StackItem.Null, nullItem);

        nullItem = new Null();
        Assert.AreEqual(StackItem.Null, nullItem);
    }

    [TestMethod]
    public void TestEqual()
    {
        StackItem itemA = "NEO";
        StackItem itemB = "NEO";
        StackItem itemC = "SmartEconomy";
        StackItem itemD = "Smarteconomy";
        StackItem itemE = "smarteconomy";

        Assert.IsTrue(itemA.Equals(itemB));
        Assert.IsFalse(itemA.Equals(itemC));
        Assert.IsFalse(itemC.Equals(itemD));
        Assert.IsFalse(itemD.Equals(itemE));
        Assert.IsFalse(itemA.Equals(new object()));
    }

    [TestMethod]
    public void TestCast()
    {
        // Signed byte

        StackItem item = sbyte.MaxValue;

        Assert.IsInstanceOfType<Integer>(item);
        Assert.AreEqual(new BigInteger(sbyte.MaxValue), ((Integer)item).GetInteger());

        // Unsigned byte

        item = byte.MaxValue;

        Assert.IsInstanceOfType<Integer>(item);
        Assert.AreEqual(new BigInteger(byte.MaxValue), ((Integer)item).GetInteger());

        // Signed short

        item = short.MaxValue;

        Assert.IsInstanceOfType<Integer>(item);
        Assert.AreEqual(new BigInteger(short.MaxValue), ((Integer)item).GetInteger());

        // Unsigned short

        item = ushort.MaxValue;

        Assert.IsInstanceOfType<Integer>(item);
        Assert.AreEqual(new BigInteger(ushort.MaxValue), ((Integer)item).GetInteger());

        // Signed integer

        item = int.MaxValue;

        Assert.IsInstanceOfType<Integer>(item);
        Assert.AreEqual(new BigInteger(int.MaxValue), ((Integer)item).GetInteger());

        // Unsigned integer

        item = uint.MaxValue;

        Assert.IsInstanceOfType<Integer>(item);
        Assert.AreEqual(new BigInteger(uint.MaxValue), ((Integer)item).GetInteger());

        // Signed long

        item = long.MaxValue;

        Assert.IsInstanceOfType<Integer>(item);
        Assert.AreEqual(new BigInteger(long.MaxValue), ((Integer)item).GetInteger());

        // Unsigned long

        item = ulong.MaxValue;

        Assert.IsInstanceOfType<Integer>(item);
        Assert.AreEqual(new BigInteger(ulong.MaxValue), ((Integer)item).GetInteger());

        // BigInteger

        item = BigInteger.MinusOne;

        Assert.IsInstanceOfType<Integer>(item);
        Assert.AreEqual(new BigInteger(-1), ((Integer)item).GetInteger());

        // Boolean

        item = true;

        Assert.IsInstanceOfType<Boolean>(item);
        Assert.IsTrue(item.GetBoolean());

        // ByteString

        item = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 };

        Assert.IsInstanceOfType<ByteString>(item);
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 }, item.GetSpan().ToArray());
    }

    [TestMethod]
    public void TestDeepCopy()
    {
        Array a = new()
        {
            true,
            1,
            new byte[] { 1 },
            StackItem.Null,
            new Buffer(new byte[] { 1 }),
            new Map { [0] = 1, [2] = 3 },
            new Struct { 1, 2, 3 }
        };
        a.Add(a);
        var aa = a.DeepCopy() as Array;
        Assert.IsNotNull(aa);
        Assert.IsFalse(a.Equals(aa, ExecutionEngineLimits.Default));
        Assert.AreSame(aa, aa[^1]);
        Assert.IsTrue(a[^2].Equals(aa[^2], ExecutionEngineLimits.Default));
        Assert.AreNotSame(a[^2], aa[^2]);
    }

    [TestMethod]
    public void TestMapRemove()
    {
        var key = 1;
        var value = "test";
        Map map = new()
        {
            [key] = value,
        };

        var removed = map.Remove(key, out _);
        Assert.AreEqual(value, removed);
        Assert.IsFalse(map.ContainsKey(key));

        removed = map.Remove(key, out _);
        Assert.IsNull(removed);

        var bigKey = new ByteString(new byte[65]);
        Assert.ThrowsExactly<ArgumentException>(() => map.Remove(bigKey, out _));

        var readonlyMap = (Map)map.DeepCopy(true);
        Assert.ThrowsExactly<InvalidOperationException>(() => readonlyMap.Remove(key, out _));
    }

    [TestMethod]
    public void TestIEquatable()
    {
        byte[] one = [1];
        StackItem expectedBoolean = true;
        StackItem expectedInteger = 1;
        StackItem expectedByteString = one;

        var expectedBuffer = new Buffer(one);
        var expectedMap = new Map { [0] = 1, [2] = 3 };
        var expectedStruct = new Struct { 1, 2, 3 };
        var expectedArray = new Array
        {
            null,
            true,
            1,
            one,
            StackItem.Null,
            new Buffer(one),
            new Map { [0] = 1, [2] = 3 },
            new Struct { 1, 2, 3 }
        };

        Boolean actualBooleanOne = true;
        Boolean actualBooleanTwo = false;
        Integer actualIntegerOne = 1;
        Integer actualIntegerTwo = 2;
        ByteString actualByteStringOne = one;
        byte[] two = [2];
        ByteString actualByteStringTwo = two;
        var actualBufferOne = new Buffer(one);
        var actualBufferTwo = new Buffer(two);
        var actualMapOne = new Map { [0] = 1, [2] = 3 };
        var actualMapTwo = new Map { [4] = 5, [6] = 7 };
        var actualStructOne = new Struct { 1, 2, 3 };
        var actualStructTwo = new Struct { 4, 5, 6 };
        var actualArrayOne = new Array
        {
            null,
            true,
            1,
            one,
            StackItem.Null,
            new Buffer(one),
            new Map { [0] = 1, [2] = 3 },
            new Struct { 1, 2, 3 }
        };
        var actualArrayTwo = new Array
        {
            new Struct { 1, 2, 3 },
            new Map { [0] = 1, [2] = 3 },
            new Buffer(one),
            StackItem.Null,
            one,
            1,
            true,
            null,
        };

        Assert.AreEqual(expectedBoolean, actualBooleanOne);
        Assert.AreEqual(expectedInteger, actualIntegerOne);
        Assert.AreEqual(expectedByteString, actualByteStringOne);

        Assert.IsTrue(expectedBuffer.Equals(actualBufferOne));
        Assert.IsTrue(expectedMap.Equals(actualMapOne));
        Assert.IsTrue(expectedStruct.Equals(actualStructOne));
        Assert.IsTrue(expectedArray.Equals(actualArrayOne));

        Assert.AreNotEqual(expectedBoolean, actualBooleanTwo);
        Assert.AreNotEqual(expectedInteger, actualIntegerTwo);
        Assert.AreNotEqual(expectedByteString, actualByteStringTwo);
        Assert.IsFalse(expectedBuffer.Equals(actualBufferTwo));
        Assert.IsFalse(expectedMap.Equals(actualMapTwo));
        Assert.IsFalse(expectedStruct.Equals(actualStructTwo));
        Assert.IsFalse(expectedArray.Equals(actualArrayTwo));

        Assert.IsFalse(expectedArray.Equals(actualArrayOne, ExecutionEngineLimits.Default));
        Assert.IsFalse(expectedBuffer.Equals(actualBufferOne, ExecutionEngineLimits.Default));
        Assert.IsFalse(expectedMap.Equals(actualMapOne, ExecutionEngineLimits.Default));
    }

    [TestMethod]
    public void TestIEquatable_MismatchBranches()
    {
        var same = new Array { 1 };
        Assert.IsTrue(same.Equals(same));

        Assert.IsFalse(new Array { 1 }.Equals(new Struct { 1 }));
        Assert.IsFalse(new Array { 1 }.Equals(new Array { 1, 2 }));
        Assert.IsFalse(new Array { 1 }.Equals(new Array { 2 }));
        Assert.IsFalse(new Array { null }.Equals(new Array { 1 }));
        Assert.IsTrue(new Array { null }.Equals(new Array { null }));

        Assert.IsFalse(new Map { [0] = 1 }.Equals(new Array { 1 }));
        Assert.IsFalse(new Map { [0] = 1 }.Equals(new Map { [0] = 1, [1] = 2 }));
        Assert.IsFalse(new Map { [0] = 1 }.Equals(new Map { [1] = 1 }));
        Assert.IsFalse(new Map { [0] = 1 }.Equals(new Map { [0] = 2 }));
        Assert.IsFalse(new Map { [0] = null }.Equals(new Map { [0] = 1 }));
        Assert.IsTrue(new Map { [0] = null }.Equals(new Map { [0] = null }));

        byte[] one = [1];
        byte[] two = [2];
        Assert.IsFalse(new Buffer(one).Equals(new Array { 1 }));
        Assert.IsFalse(new Buffer(one).Equals(new Buffer(two)));
        Assert.IsTrue(new Buffer(one).Equals(new Buffer(one)));

        var s = new Struct { 1 };
        Assert.IsTrue(s.Equals(new Struct { 1 }));
        Assert.IsFalse(s.Equals(new Struct { 2 }));
        Assert.IsFalse(s.Equals(new Array { 1 }));
        Assert.IsTrue(s.Equals(s, ExecutionEngineLimits.Default));
    }

    [TestMethod]
    public void HasCircularReference_DetectsSelfReference()
    {
        var a = new Array();
        a.Add(a);
        Assert.IsTrue(a.HasCircularReference());
        Assert.IsFalse(new Array { 1, 2 }.HasCircularReference());

        var s = new Struct();
        s.Add(s);
        Assert.IsTrue(s.HasCircularReference());
        Assert.IsFalse(new Struct { 1, 2 }.HasCircularReference());
    }

    [TestMethod]
    public void HasCircularReference_DiamondIsNotACycle()
    {
        var child = new Array { 1 };
        var parent = new Array { child, child };
        Assert.IsFalse(parent.HasCircularReference());
    }

    [TestMethod]
    public void TestIEquatable_CircularArray()
    {
        var a = new Array();
        a.Add(a);
        var b = new Array();
        b.Add(b);
        Assert.IsTrue(a.Equals(a));
        Assert.IsTrue(a.Equals(b));
        Assert.IsFalse(a.Equals(b, ExecutionEngineLimits.Default));

        var c = new Array { 1 };
        c.Add(c);
        var d = new Array { 2 };
        d.Add(d);
        Assert.IsFalse(c.Equals(d));

        var leftOuter = new Array();
        var leftInner = new Array();
        leftOuter.Add(leftInner);
        leftInner.Add(leftOuter);
        var rightOuter = new Array();
        var rightInner = new Array();
        rightOuter.Add(rightInner);
        rightInner.Add(rightOuter);
        Assert.IsTrue(leftOuter.Equals(rightOuter));
    }

    [TestMethod]
    public void TestIEquatable_CircularStruct()
    {
        var s1 = new Struct();
        s1.Add(s1);
        var s2 = new Struct();
        s2.Add(s2);
        Assert.IsTrue(s1.Equals(s2));
        Assert.ThrowsExactly<InvalidOperationException>(() => s1.Equals(s2, ExecutionEngineLimits.Default));

        var t1 = new Struct { 1 };
        t1.Add(t1);
        var t2 = new Struct { 2 };
        t2.Add(t2);
        Assert.IsFalse(t1.Equals(t2));
        Assert.ThrowsExactly<InvalidOperationException>(() => t1.Equals(t2, ExecutionEngineLimits.Default));
    }

    [TestMethod]
    public void EqualOpcode_ArraysStayReferenceEquality()
    {
        using var engine = new ExecutionEngine();
        using var sb = new ScriptBuilder();
        sb.EmitPush(1);
        sb.EmitPush(1);
        sb.Emit(OpCode.PACK);
        sb.EmitPush(1);
        sb.EmitPush(1);
        sb.Emit(OpCode.PACK);
        sb.Emit(OpCode.EQUAL);
        engine.LoadScript(sb.ToArray());
        Assert.AreEqual(VMState.HALT, engine.Execute());
        Assert.IsFalse(engine.ResultStack.Pop().GetBoolean());
    }

    [TestMethod]
    public void TestIEquatable_NestedArrayBranches()
    {
        Assert.IsFalse(new Array { 1 }.Equals((StackItem)1));
        byte[] oneByte = [1];
        Assert.IsFalse(new Array { 1 }.Equals(new Buffer(oneByte)));
        Assert.IsFalse(new Array { 1 }.Equals(new Map { [0] = 1 }));

        var nestedMatch = new Array { new Array { 1, 2 } };
        var nestedSame = new Array { new Array { 1, 2 } };
        var nestedMismatch = new Array { new Array { 1, 3 } };
        var nestedNotArray = new Array { 1 };
        Assert.IsTrue(nestedMatch.Equals(nestedSame));
        Assert.IsFalse(nestedMatch.Equals(nestedMismatch));
        Assert.IsFalse(nestedMatch.Equals(nestedNotArray));

        var shared = new Array { 1 };
        var diamond = new Array { shared, shared };
        var diamond2 = new Array { shared, shared };
        Assert.IsTrue(diamond.Equals(diamond2));

        var a = new Array();
        a.Add(a);
        a.Add(a);
        var b = new Array();
        var c = new Array();
        b.Add(b);
        b.Add(c);
        Assert.IsFalse(a.Equals(b));

        byte[] one = [1];
        Assert.IsTrue(new Array { new Buffer(one) }.Equals(new Array { new Buffer(one) }));
        Assert.IsTrue(new Array { new Map { [0] = 1 } }.Equals(new Array { new Map { [0] = 1 } }));
    }

    [TestMethod]
    public void TestIEquatable_StructBranches()
    {
        Assert.IsTrue(new Struct { (ByteString)"ab" }.Equals(new Struct { (ByteString)"ab" }));
        Assert.IsFalse(new Struct { (ByteString)"ab" }.Equals(new Struct { (ByteString)"cd" }));
        Assert.IsFalse(new Struct { (ByteString)"a" }.Equals(new Struct { 1 }));

        var inner = new Struct { 1 };
        Assert.IsTrue(new Struct { inner }.Equals(new Struct { inner }));
        Assert.IsFalse(new Struct { new Struct { 1 } }.Equals(new Struct { new Array { 1 } }));
        Assert.IsFalse(new Struct { 1 }.Equals(new Struct { 1, 2 }));

        var nestedArray = new Struct { new Array { 1, 2 } };
        Assert.IsTrue(nestedArray.Equals(new Struct { new Array { 1, 2 } }));
        Assert.IsFalse(nestedArray.Equals(new Struct { new Array { 1, 3 } }));
        Assert.IsFalse(nestedArray.Equals(new Struct { 1 }));

        var left = new Struct();
        left.Add(left);
        left.Add(left);
        var right = new Struct();
        right.Add(right);
        right.Add(new Struct());
        Assert.IsFalse(left.Equals(right));

        var tiny = ExecutionEngineLimits.Default with { MaxComparableSize = 0 };
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            new Struct { 1 }.Equals(new Struct { 1 }, tiny));

        var shallow = ExecutionEngineLimits.Default with { MaxStackSize = 1 };
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            new Struct { 1 }.Equals(new Struct { 1 }, shallow));

        var tooBig = ExecutionEngineLimits.Default with { MaxComparableSize = 1 };
        byte[] bigBytes = [1, 2, 3];
        ByteString big = bigBytes;
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            new Struct { big }.Equals(new Struct { big }, tooBig));

        byte[] smallBytes = [1];
        ByteString small = smallBytes;
        var otherTooBig = ExecutionEngineLimits.Default with { MaxComparableSize = 2 };
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            small.Equals(big, otherTooBig));
        Assert.IsFalse(small.Equals((StackItem)1));
    }

    [TestMethod]
    public void TestIEquatable_MapAndBufferSelf()
    {
        var map = new Map { [0] = 1 };
        Assert.IsTrue(map.Equals(map));
        Assert.IsTrue(new Map { [0] = new Array { 1 } }.Equals(new Map { [0] = new Array { 1 } }));
        Assert.IsFalse(new Map { [0] = new Array { 1 } }.Equals(new Map { [0] = new Array { 2 } }));

        byte[] one = [1];
        var buffer = new Buffer(one);
        Assert.IsTrue(buffer.Equals(buffer));
        Assert.IsFalse(buffer.Equals((StackItem)1));
    }

    [TestMethod]
    public void HasCircularReference_MapNullAndPrimitive()
    {
        var map = new Map();
        map[1] = map;
        Assert.IsTrue(map.HasCircularReference());
        Assert.IsFalse(new Map { [1] = 2 }.HasCircularReference());

        var withNull = new Array { null };
        Assert.IsFalse(withNull.HasCircularReference());

        var outer = new Array();
        var inner = new Array();
        outer.Add(inner);
        inner.Add(outer);
        Assert.IsTrue(outer.HasCircularReference());

        StackItem number = 1;
        Assert.IsFalse(number.HasCircularReference());
        Assert.IsFalse(StackItem.Null.HasCircularReference());
    }
}
