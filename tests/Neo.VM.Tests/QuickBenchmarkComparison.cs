// Copyright (C) 2015-2026 The Neo Project.
//
// QuickBenchmarkComparison.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using Neo.VM.Types;
using System;
using System.Diagnostics;
using SysArray = System.Array;
using VMArray = Neo.VM.Types.Array;
using VMBuffer = Neo.VM.Types.Buffer;

namespace Neo.VM.Benchmarks;

public static class QuickBenchmarkComparison
{
    public static void Run()
    {
        var rc = new ReferenceCounter();

        Console.WriteLine("=== Neo.VM Performance Comparison ===");
        Console.WriteLine("Testing actual differences in implementation\n");

        // Test 1: Array Creation - comparing old approach vs new optimized constructor
        Console.WriteLine("Test 1: Array Creation (100,000 iterations)");
        Console.WriteLine("  Old approach: Create temp array + Fill + Wrap");
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < 100000; i++)
        {
            var tempArray = new StackItem[16];
            SysArray.Fill(tempArray, StackItem.Null);
            var arr = new VMArray(rc, tempArray);
        }
        sw1.Stop();
        Console.WriteLine($"    Time: {sw1.ElapsedMilliseconds}ms");

        Console.WriteLine("  New approach: Optimized constructor (skipRefCounting)");
        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < 100000; i++)
        {
            var arr = new VMArray(rc, StackItem.Null, 16, skipReferenceCounting: true);
        }
        sw2.Stop();
        Console.WriteLine($"    Time: {sw2.ElapsedMilliseconds}ms");
        var speedup1 = sw2.ElapsedMilliseconds > 0 ? (double)sw1.ElapsedMilliseconds / sw2.ElapsedMilliseconds : 0;
        Console.WriteLine($"    Speedup: {speedup1:F2}x faster\n");

        // Test 2: Reference Counting - Single vs Bulk
        Console.WriteLine("Test 2: Reference Counting (100,000 × 16 refs)");
        var refItem = Integer.Zero;
        var parent1 = new VMArray(rc);
        for (int i = 0; i < 16; i++) parent1.Add(Integer.Zero);

        var sw3 = Stopwatch.StartNew();
        for (int i = 0; i < 100000; i++)
        {
            for (int j = 0; j < 16; j++)
            {
                rc.AddReference(refItem, parent1);
            }
        }
        sw3.Stop();
        Console.WriteLine($"  Old approach (single adds): {sw3.ElapsedMilliseconds}ms");

        var parent2 = new VMArray(rc);
        for (int i = 0; i < 16; i++) parent2.Add(Integer.Zero);

        var sw4 = Stopwatch.StartNew();
        for (int i = 0; i < 100000; i++)
        {
            rc.AddReference(refItem, parent2, 16);
        }
        sw4.Stop();
        Console.WriteLine($"  New approach (bulk add): {sw4.ElapsedMilliseconds}ms");
        var speedup2 = sw4.ElapsedMilliseconds > 0 ? (double)sw3.ElapsedMilliseconds / sw4.ElapsedMilliseconds : sw3.ElapsedMilliseconds;
        Console.WriteLine($"  Speedup: {speedup2:F2}x faster\n");

        // Test 3: HasTrackableSubItems - manual check vs property
        Console.WriteLine("Test 3: HasTrackableSubItems Check (1,000,000 iterations)");
        var arrWithTrackable = new VMArray(rc);
        for (int i = 0; i < 16; i++)
            arrWithTrackable.Add(new VMBuffer(16));

        Console.WriteLine("  Old approach: Manual iteration through all items");
        var sw5 = Stopwatch.StartNew();
        for (int i = 0; i < 1000000; i++)
        {
            bool hasTrackable = false;
            foreach (var subItem in arrWithTrackable)
            {
                if (subItem is CompoundType or VMBuffer)
                {
                    hasTrackable = true;
                    break;
                }
            }
            _ = hasTrackable; // Use the variable to avoid warning
        }
        sw5.Stop();
        Console.WriteLine($"    Time: {sw5.ElapsedMilliseconds}ms");

        Console.WriteLine("  New approach: Property check");
        var sw6 = Stopwatch.StartNew();
        for (int i = 0; i < 1000000; i++)
        {
            _ = arrWithTrackable.HasTrackableSubItems;
        }
        sw6.Stop();
        Console.WriteLine($"    Time: {sw6.ElapsedMilliseconds}ms");
        var speedup3 = sw6.ElapsedMilliseconds > 0 ? (double)sw5.ElapsedMilliseconds / sw6.ElapsedMilliseconds : 0;
        Console.WriteLine($"    Speedup: {speedup3:F2}x faster\n");

        // Summary
        Console.WriteLine("=== Summary ===");
        Console.WriteLine($"✅ Array Creation: {speedup1:F2}x faster with optimized constructor");
        Console.WriteLine($"✅ Bulk Reference Counting: {speedup2:F2}x faster");
        Console.WriteLine($"HasTrackableSubItems: {speedup3:F2}x vs manual loop");
        Console.WriteLine("\nValidate overall impact with targeted benchmarks for your workload");
    }
}
