// Copyright (C) 2015-2026 The Neo Project.
//
// Benchmarks_DeepCopy.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Array = Neo.VM.Types.Array;

namespace Neo.VM.Benchmarks.VMTypes;

public class Benchmarks_DeepCopy
{
    public static IEnumerable<(int Depth, int ElementsPerLevel)> ParamSource()
    {
        int[] depths = [2, 4];
        int[] elementsPerLevel = [2, 4, 6];

        foreach (var depth in depths)
        {
            foreach (var elements in elementsPerLevel)
            {
                if (depth <= 8 || elements <= 2)
                {
                    yield return (depth, elements);
                }
            }
        }
    }

    [ParamsSource(nameof(ParamSource))]
    public (int Depth, int ElementsPerLevel) Params;

    [Benchmark]
    public void BenchNestedArrayDeepCopy()
    {
        var root = new Array();
        CreateNestedArray(root, Params.Depth, Params.ElementsPerLevel);
        _ = root.DeepCopy();
    }

    [Benchmark]
    public void BenchNestedTestArrayDeepCopy()
    {
        var root = new TestArray();
        CreateNestedTestArray(root, Params.Depth, Params.ElementsPerLevel);
        _ = root.DeepCopy();
    }

    private static void CreateNestedArray(Array? rootArray, int depth, int elementsPerLevel = 1)
    {
        if (depth < 0)
        {
            throw new ArgumentException("Depth must be non-negative", nameof(depth));
        }

        ArgumentNullException.ThrowIfNull(rootArray);

        if (depth == 0)
        {
            return;
        }

        for (var i = 0; i < elementsPerLevel; i++)
        {
            var childArray = new Array();
            rootArray.Add(childArray);
            CreateNestedArray(childArray, depth - 1, elementsPerLevel);
        }
    }

    private static void CreateNestedTestArray(TestArray rootArray, int depth, int elementsPerLevel = 1)
    {
        if (depth < 0)
        {
            throw new ArgumentException("Depth must be non-negative", nameof(depth));
        }

        ArgumentNullException.ThrowIfNull(rootArray);

        if (depth == 0)
        {
            return;
        }

        for (var i = 0; i < elementsPerLevel; i++)
        {
            var childArray = new TestArray();
            rootArray.Add(childArray);
            CreateNestedTestArray(childArray, depth - 1, elementsPerLevel);
        }
    }
}
