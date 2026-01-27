// Copyright (C) 2015-2026 The Neo Project.
//
// Benchmarks_ArrayComparison.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.VM.Types;
using SysArray = System.Array;

namespace Neo.VM.Benchmarks.OpCodes;

/// <summary>
/// Direct comparison benchmarks showing the performance improvements
/// from the Array optimization (List→Array with pooling).
/// </summary>
[MemoryDiagnoser]
[InvocationCount(1)]
[IterationCount(15)]
[WarmupCount(5)]
public class Benchmarks_ArrayComparison
{
    private const int ArraySize = 64;
    private const int Iterations = 10000;

    private ReferenceCounter _rc = null!;

    [GlobalSetup]
    public void Setup()
    {
        _rc = new ReferenceCounter();
    }

    // NEW OPTIMIZED IMPLEMENTATION
    [Benchmark(Baseline = true)]
    public void NewImplementation_WithPooling()
    {
        for (int i = 0; i < Iterations; i++)
        {
            var arr = new Neo.VM.Types.Array(_rc, StackItem.Null, ArraySize, skipReferenceCounting: true);
        }
    }

    // LEGACY IMPLEMENTATION (simulated for comparison)
    [Benchmark]
    public void LegacyImplementation_WithList()
    {
        for (int i = 0; i < Iterations; i++)
        {
            var items = new StackItem[ArraySize];
            SysArray.Fill(items, StackItem.Null);
            var list = new System.Collections.Generic.List<StackItem>(items);
            foreach (var item in list)
                _rc.AddStackReference(item);
        }
    }

    [Benchmark]
    public void CreateAndDispose_Array()
    {
        for (int i = 0; i < Iterations; i++)
        {
            var arr = new Neo.VM.Types.Array(_rc, StackItem.Null, ArraySize, skipReferenceCounting: false);
            // Simulate usage
            for (int j = 0; j < ArraySize; j++)
            {
                var item = arr[j];
            }
            // Array is pooled, will be returned when GC'd
        }
    }

    [Benchmark]
    public void AddRemoveItems_Array()
    {
        for (int i = 0; i < Iterations; i++)
        {
            var arr = new Neo.VM.Types.Array(_rc, StackItem.Null, ArraySize, skipReferenceCounting: false);
            // Access all items
            for (int j = 0; j < ArraySize; j++)
            {
                _ = arr[j];
            }
        }
    }
}
