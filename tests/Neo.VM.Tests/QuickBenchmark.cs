// Copyright (C) 2015-2026 The Neo Project.
//
// QuickBenchmark.cs file belongs to the neo project and is free
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
using VMArray = Neo.VM.Types.Array;
using VMBuffer = Neo.VM.Types.Buffer;

namespace Neo.VM.Benchmarks;

public static class QuickBenchmark
{
    public static void Run()
    {
        var rc = new ReferenceCounter();
        int iterations = 1000000;  // Increased for more measurable results

        Console.WriteLine("=== Neo.VM Performance Optimization Benchmark ===\n");

        // Test 1: Array Creation with optimized constructor
        Console.WriteLine($"Test 1: Array Creation (optimized constructor) ({iterations:N0} iterations)");
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var arr = new VMArray(rc, StackItem.Null, 16, skipReferenceCounting: true);
        }
        sw.Stop();
        var arrayMs = sw.ElapsedMilliseconds;
        var arrayPerIter = (double)sw.ElapsedTicks / iterations;
        Console.WriteLine($"  Time: {arrayMs}ms");
        Console.WriteLine($"  Per iteration: {arrayPerIter:F4} ticks");
        Console.WriteLine($"  Throughput: {iterations / (double)sw.ElapsedMilliseconds:F0} ops/ms");
        Console.WriteLine($"  Status: ✅ Uses optimized constructor (bulk fill)\n");

        // Test 2: Bulk Reference Counting
        int refIterations = 100000;  // Increased iterations
        Console.WriteLine($"Test 2: Bulk Reference Counting ({refIterations:N0} × 16 refs)");
        var item = Integer.Zero;
        var parent = new VMArray(rc, StackItem.Null, 16, skipReferenceCounting: true);

        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < refIterations; i++)
        {
            for (int j = 0; j < 16; j++)
            {
                rc.AddReference(item, parent);
            }
        }
        sw1.Stop();

        var parent2 = new VMArray(rc, StackItem.Null, 16, skipReferenceCounting: true);
        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < refIterations; i++)
        {
            rc.AddReference(item, parent2, 16);
        }
        sw2.Stop();

        var singleMs = sw1.ElapsedMilliseconds;
        var bulkMs = sw2.ElapsedMilliseconds;
        Console.WriteLine($"  Single adds: {singleMs}ms");
        Console.WriteLine($"  Bulk adds:   {bulkMs}ms");
        var speedup = bulkMs > 0 ? (double)singleMs / bulkMs : singleMs;
        Console.WriteLine($"  Speedup: {speedup:F2}x faster");
        Console.WriteLine($"  Total refs: {refIterations * 16:N0}\n");

        // Test 3: Map Keys/Values Access
        int mapIterations = 100000;
        Console.WriteLine($"Test 3: Map Keys/Values Access ({mapIterations:N0} iterations)");
        var map = new Map(rc);
        for (int i = 0; i < 100; i++)
            map[i] = new Integer(i);

        sw = Stopwatch.StartNew();
        for (int i = 0; i < mapIterations; i++)
        {
            var keys = map.Keys;
            var values = map.Values;
        }
        sw.Stop();
        Console.WriteLine($"  Time: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"  Throughput: {mapIterations / (double)sw.ElapsedMilliseconds:F0} ops/ms");
        Console.WriteLine("  Status: Map Keys/Values access timing\n");

        // Test 4: HasTrackableSubItems Check
        int checkIterations = 1000000;
        Console.WriteLine($"Test 4: HasTrackableSubItems Check ({checkIterations:N0} iterations)");
        var arrWithTrackable = new VMArray(rc);
        for (int i = 0; i < 16; i++)
            arrWithTrackable.Add(new VMBuffer(16));

        var arrWithout = new VMArray(rc);
        for (int i = 0; i < 16; i++)
            arrWithout.Add(Integer.Zero);

        sw = Stopwatch.StartNew();
        for (int i = 0; i < checkIterations; i++)
        {
            _ = arrWithTrackable.HasTrackableSubItems;
            _ = arrWithout.HasTrackableSubItems;
        }
        sw.Stop();
        Console.WriteLine($"  Time: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"  Throughput: {checkIterations / (double)sw.ElapsedMilliseconds:F0} checks/ms");
        Console.WriteLine("  Status: Property check timing (may scan items)\n");

        // Summary
        Console.WriteLine("=== Summary ===");
        Console.WriteLine($"✅ Array constructor (bulk fill): {arrayPerIter:F4} ticks/iter, {iterations / (double)arrayMs:F0} ops/ms");
        Console.WriteLine($"✅ Bulk reference counting: {speedup:F2}x faster for {refIterations * 16:N0} operations");
        Console.WriteLine("HasTrackableSubItems: property access timing recorded above");
        Console.WriteLine("\nValidate overall impact with targeted benchmarks for your workload");
    }
}
