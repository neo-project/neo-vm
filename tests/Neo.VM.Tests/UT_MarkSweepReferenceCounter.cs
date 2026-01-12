// Copyright (C) 2015-2026 The Neo Project.
//
// UT_MarkSweepReferenceCounter.cs file belongs to the neo project and is free
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

namespace Neo.Test;

[TestClass]
public class UT_MarkSweepReferenceCounter
{
    [TestMethod]
    public void TestReachableChildIsNotCollected()
    {
        var rc = new MarkSweepReferenceCounter();

        var root = new Array(rc);
        var child = new Array(rc);

        rc.AddStackReference(root);
        child.Add(1);
        child.Add(2);
        child.Add(3);

        root.Add(child);

        Assert.AreEqual(5, rc.Count);
        rc.CheckZeroReferred();
        Assert.AreEqual(5, rc.Count);
    }
}

