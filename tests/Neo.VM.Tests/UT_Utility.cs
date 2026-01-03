// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Utility.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using System;
using System.Numerics;

namespace Neo.Test;

[TestClass]
public class UT_Utility
{
    [TestMethod]
    public void SqrtTest()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => BigInteger.MinusOne.Sqrt());

        Assert.AreEqual(BigInteger.Zero, BigInteger.Zero.Sqrt());
        Assert.AreEqual(new BigInteger(1), new BigInteger(1).Sqrt());
        Assert.AreEqual(new BigInteger(1), new BigInteger(2).Sqrt());
        Assert.AreEqual(new BigInteger(1), new BigInteger(3).Sqrt());
        Assert.AreEqual(new BigInteger(2), new BigInteger(4).Sqrt());
        Assert.AreEqual(new BigInteger(9), new BigInteger(81).Sqrt());
    }

    [TestMethod]
    public void ModInverseTest()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BigInteger.One.ModInverse(BigInteger.Zero));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BigInteger.One.ModInverse(BigInteger.One));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BigInteger.Zero.ModInverse(BigInteger.Zero));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BigInteger.Zero.ModInverse(BigInteger.One));
        Assert.ThrowsExactly<InvalidOperationException>(() => new BigInteger(ushort.MaxValue).ModInverse(byte.MaxValue));

        Assert.AreEqual(new BigInteger(52), new BigInteger(19).ModInverse(141));
    }
}
