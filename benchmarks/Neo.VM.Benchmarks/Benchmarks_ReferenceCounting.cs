// Copyright (C) 2015-2026 The Neo Project.
//
// Benchmarks_ReferenceCounting.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.VM.Types;
using VMArray = Neo.VM.Types.Array;
using VMBuffer = Neo.VM.Types.Buffer;

namespace Neo.VM.Benchmarks.OpCodes;

/// <summary>
/// Benchmarks for reference counting improvements, specifically
/// the new bulk AddReference method and HasTrackableSubItems optimization.
/// </summary>
[MemoryDiagnoser]
[InvocationCount(1)]
[IterationCount(15)]
[WarmupCount(5)]
public class Benchmarks_ReferenceCounting
{
    private const int ArraySize = 16;
    private const int Iterations = 1000;

    private ReferenceCounter _rc = null!;
    private StackItem _item = null!;

    [GlobalSetup]
    public void Setup()
    {
        _rc = new ReferenceCounter();
        _item = new Integer(42);
    }

    // BULK ADDREFERENCE BENCHMARK
    [Benchmark(Baseline = true)]
    public void AddReference_SingleCalls()
    {
        var parent = new VMArray(_rc);
        for (int i = 0; i < ArraySize; i++)
        {
            parent.Add(_item);
        }
    }

    [Benchmark]
    public void AddReference_BulkCall()
    {
        var parent = new VMArray(_rc, StackItem.Null, ArraySize, skipReferenceCounting: true);
        _rc.AddReference(_item, parent, ArraySize);
    }

    // HAS_TRACKABLE_SUBITEMS BENCHMARK
    [Benchmark]
    public void ReferenceCount_WithTrackableItems()
    {
        var arr = new VMArray(_rc);
        for (int i = 0; i < ArraySize; i++)
        {
            arr.Add(new VMBuffer(16));
        }
        // HasTrackableSubItems check is now O(1) instead of O(n)
        _ = (arr as CompoundType)!.HasTrackableSubItems;
    }

    [Benchmark]
    public void ReferenceCount_WithoutTrackableItems()
    {
        var arr = new VMArray(_rc);
        for (int i = 0; i < ArraySize; i++)
        {
            arr.Add(Integer.Zero);
        }
        // HasTrackableSubItems check is now O(1) instead of O(n)
        _ = (arr as CompoundType)!.HasTrackableSubItems;
    }

    // CREATE MANY ARRAYS
    [Benchmark]
    public void CreateManyArrays_Small()
    {
        for (int i = 0; i < Iterations; i++)
        {
            var arr = new VMArray(_rc, StackItem.Null, 8, skipReferenceCounting: false);
            // Array will use pooling
        }
    }

    [Benchmark]
    public void CreateManyArrays_Large()
    {
        for (int i = 0; i < Iterations / 10; i++)
        {
            var arr = new VMArray(_rc, StackItem.Null, 128, skipReferenceCounting: false);
            // Array will use pooling
        }
    }
}
