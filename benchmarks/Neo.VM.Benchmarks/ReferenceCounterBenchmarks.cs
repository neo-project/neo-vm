// Copyright (C) 2015-2026 The Neo Project.
//
// ReferenceCounterBenchmarks.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using Array = Neo.VM.Types.Array;

namespace Neo.VM.Benchmarks;

[SimpleJob]
[MemoryDiagnoser]
public class ReferenceCounterBenchmarks
{
    public enum Workload
    {
        NestedArrays,
        DenseCycles,
        StackChurn,
        MapHeavy
    }

    [Params(nameof(ReferenceCounter), nameof(MarkSweepReferenceCounter))]
    public string Strategy { get; set; } = nameof(ReferenceCounter);

    [Params(Workload.NestedArrays, Workload.DenseCycles, Workload.StackChurn, Workload.MapHeavy)]
    public Workload Scenario { get; set; }

    [Params(32)]
    public int RootCount { get; set; }

    [Params(4)]
    public int Depth { get; set; }

    [Params(4)]
    public int FanOut { get; set; }

    [Params(1024)]
    public int Iterations { get; set; }

    [Benchmark]
    public int ExecuteScenario()
    {
        return Scenario switch
        {
            Workload.NestedArrays => CollectNestedArrays(),
            Workload.DenseCycles => CollectDenseCycles(),
            Workload.StackChurn => CollectStackChurn(),
            Workload.MapHeavy => CollectMapHeavy(),
            _ => throw new ArgumentOutOfRangeException(nameof(Scenario))
        };
    }

    private int CollectNestedArrays()
    {
        var counter = CreateCounter();
        var roots = new Array[RootCount];

        for (int i = 0; i < RootCount; i++)
        {
            var root = new Array(counter);
            counter.AddStackReference(root);
            roots[i] = root;
            BuildNested(root, Depth, counter);
        }

        foreach (var root in roots)
            counter.RemoveStackReference(root);

        return counter.CheckZeroReferred();
    }

    private void BuildNested(Array current, int depth, IReferenceCounter counter)
    {
        if (depth == 0) return;

        var child = new Array(counter);
        current.Add(child);
        child.Add(current);

        var sibling = new Array(counter);
        current.Add(sibling);
        sibling.Add(child);

        BuildNested(child, depth - 1, counter);
    }

    private int CollectDenseCycles()
    {
        var counter = CreateCounter();
        var roots = new Array[RootCount];
        for (int i = 0; i < RootCount; i++)
        {
            roots[i] = new Array(counter);
            counter.AddStackReference(roots[i]);
        }

        foreach (var root in roots)
            BuildDenseCycles(root, Depth, counter, roots);

        foreach (var root in roots)
            counter.RemoveStackReference(root);

        return counter.CheckZeroReferred();
    }

    private void BuildDenseCycles(Array parent, int depth, IReferenceCounter counter, Array[] roots)
    {
        if (depth == 0) return;
        for (int i = 0; i < FanOut; i++)
        {
            var child = new Array(counter);
            parent.Add(child);
            child.Add(parent);
            if (i % 2 == 0)
                child.Add(roots[(i + parent.Count) % roots.Length]);
            BuildDenseCycles(child, depth - 1, counter, roots);
        }
    }

    private int CollectStackChurn()
    {
        var counter = CreateCounter();
        List<Array> window = new();
        for (int i = 0; i < Iterations; i++)
        {
            var node = new Array(counter);
            counter.AddStackReference(node);
            if (window.Count > 0)
                node.Add(window[i % window.Count]);
            window.Add(node);
            if (window.Count > FanOut)
            {
                var victim = window[0];
                counter.RemoveStackReference(victim);
                window.RemoveAt(0);
            }
        }

        foreach (var node in window)
            counter.RemoveStackReference(node);

        return counter.CheckZeroReferred();
    }

    private int CollectMapHeavy()
    {
        var counter = CreateCounter();
        var roots = new Map[RootCount];

        for (int i = 0; i < RootCount; i++)
        {
            var root = new Map(counter);
            counter.AddStackReference(root);
            roots[i] = root;
        }

        for (int i = 0; i < RootCount; i++)
            BuildMapHeavy(roots[i], Depth, counter, roots, (i + 1) * 1_000_000);

        foreach (var root in roots)
            counter.RemoveStackReference(root);

        return counter.CheckZeroReferred();
    }

    private void BuildMapHeavy(Map parent, int depth, IReferenceCounter counter, Map[] roots, int seed)
    {
        if (depth == 0) return;

        int keyBase = seed + depth * 1_000;
        for (int i = 0; i < FanOut; i++)
        {
            int key = keyBase + i;
            if ((i & 1) == 0)
            {
                var childMap = new Map(counter);
                parent[key] = childMap;
                childMap[key + 100_000] = parent;
                childMap[key + 200_000] = roots[(key + i) % roots.Length];

                var array = new Array(counter)
                {
                    childMap
                };
                childMap[key + 300_000] = array;

                BuildMapHeavy(childMap, depth - 1, counter, roots, keyBase + (i + 1) * 10_000);
            }
            else
            {
                var childArray = new Array(counter)
                {
                    parent,
                    roots[(key + i) % roots.Length]
                };
                parent[key] = childArray;
            }
        }
    }

    private IReferenceCounter CreateCounter() => Strategy switch
    {
        nameof(ReferenceCounter) => new ReferenceCounter(),
        nameof(MarkSweepReferenceCounter) => new MarkSweepReferenceCounter(),
        _ => throw new ArgumentOutOfRangeException(nameof(Strategy))
    };
}
