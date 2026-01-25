// Copyright (C) 2015-2026 The Neo Project.
//
// UT_QuickBenchmarkComparison.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM.Benchmarks;
using System;
using System.IO;

namespace Neo.Test;

[TestClass]
[Ignore("Performance benchmark")]
public class UT_QuickBenchmarkComparison
{
    [TestMethod]
    public void RunPerformanceComparison()
    {
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            QuickBenchmarkComparison.Run();
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var output = sw.ToString();
        Console.Write(output);

        // Also write to file for reference
        File.WriteAllText("/tmp/neo-vm-comparison-results.txt", output);

        Assert.IsFalse(string.IsNullOrEmpty(output));
    }
}
