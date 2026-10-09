// Copyright (C) 2015-2026 The Neo Project.
//
// UT_MemoryOwner.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM.Types;
using System;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.Test;

[TestClass]
public class UT_MemoryOwner
{
    [TestMethod]
    public void ByteString_CopiesIntoPooledMemory()
    {
        byte[] data = [1, 2, 3];
        var item = new ByteString(data);
        data[0] = 9;
        byte[] expected = [1, 2, 3];
        CollectionAssert.AreEqual(expected, item.GetSpan().ToArray());
    }

    [TestMethod]
    public void ByteString_Empty_HasNoOwner()
    {
        Assert.AreEqual(0, ByteString.Empty.Size);
        ByteString.Empty.Cleanup();
        Assert.AreEqual(0, ByteString.Empty.GetSpan().Length);
    }

    [TestMethod]
    public void Buffer_UsesPooledSlice()
    {
        var buffer = new Buffer(4);
        Assert.AreEqual(4, buffer.Size);
        byte[] zeros = [0, 0, 0, 0];
        CollectionAssert.AreEqual(zeros, buffer.GetSpan().ToArray());

        byte[] data = [1, 2, 3];
        var copied = new Buffer(data);
        data[0] = 9;
        byte[] copiedExpected = [1, 2, 3];
        CollectionAssert.AreEqual(copiedExpected, copied.GetSpan().ToArray());
    }

    [TestMethod]
    public void Buffer_ZeroSize_AndNegative()
    {
        var empty = new Buffer(0);
        Assert.AreEqual(0, empty.Size);
        empty.Cleanup();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new Buffer(-1));
    }

    [TestMethod]
    public void Buffer_KeepAlive_DoesNotDispose()
    {
        var buffer = new Buffer(2);
        buffer.InnerBuffer.Span[0] = 7;
        buffer.KeepAlive();
        buffer.Cleanup();
        Assert.AreEqual(7, buffer.InnerBuffer.Span[0]);
    }
}
