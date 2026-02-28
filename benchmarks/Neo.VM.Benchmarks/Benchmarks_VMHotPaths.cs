// Copyright (C) 2015-2026 The Neo Project.
//
// Benchmarks_VMHotPaths.cs file belongs to the neo project and is free
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
using System.Linq;
using VMArray = Neo.VM.Types.Array;

namespace Neo.VM.Benchmarks;

public class Benchmarks_ArrayBuild
{
    [Params(1, 8, 32, 256, 1024)]
    public int N { get; set; }

    private static readonly StackItem Item = StackItem.Null;

    [Benchmark]
    public VMArray BuildWithListFill()
    {
        var referenceCounter = new ReferenceCounter();
        var items = new List<StackItem>(N);
        for (int i = 0; i < N; i++)
            items.Add(Item);
        return new VMArray(referenceCounter, items);
    }

    [Benchmark]
    public VMArray BuildWithArrayFill()
    {
        var referenceCounter = new ReferenceCounter();
        var itemArray = new StackItem[N];
        System.Array.Fill(itemArray, Item);
        return new VMArray(referenceCounter, itemArray);
    }
}

public class Benchmarks_MapSubItems
{
    [Params(1, 8, 32, 256, 1024)]
    public int N { get; set; }

    private Map _map = null!;

    [GlobalSetup]
    public void Setup()
    {
        _map = new Map();
        for (int i = 0; i < N; i++)
            _map[new Integer(i)] = new Integer(i + 1);
    }

    [Benchmark]
    public int EnumerateYield()
    {
        int count = 0;
        foreach (var _ in SubItemsYield(_map))
            count++;
        return count;
    }

    [Benchmark]
    public int EnumerateConcat()
    {
        int count = 0;
        foreach (var _ in SubItemsConcat(_map))
            count++;
        return count;
    }

    private static IEnumerable<StackItem> SubItemsYield(Map map)
    {
        foreach (var key in map.Keys)
            yield return key;
        foreach (var value in map.Values)
            yield return value;
    }

    private static IEnumerable<StackItem> SubItemsConcat(Map map) => map.Keys.Concat(map.Values);
}

public class Benchmarks_ListCopy
{
    private const int SourceCount = 4096;

    [Params(1, 8, 32, 256, 1024, 2048)]
    public int CopyCount { get; set; }

    private List<StackItem> _source = null!;
    private List<StackItem> _target = null!;

    [GlobalSetup]
    public void Setup()
    {
        _source = new List<StackItem>(SourceCount);
        for (int i = 0; i < SourceCount; i++)
            _source.Add(StackItem.Null);
        _target = new List<StackItem>(CopyCount);
    }

    [Benchmark]
    public int AddRangeSkip()
    {
        _target.Clear();
        _target.AddRange(_source.Skip(_source.Count - CopyCount));
        return _target.Count;
    }

    [Benchmark]
    public int ManualLoop()
    {
        _target.Clear();
        int start = _source.Count - CopyCount;
        for (int i = start; i < _source.Count; i++)
            _target.Add(_source[i]);
        return _target.Count;
    }
}

public class Benchmarks_InstructionAdvance
{
    [Params(1024, 4096, 16384)]
    public int InstructionCount { get; set; }

    private Script _script = null!;
    private ExecutionContext _context = null!;

    [GlobalSetup]
    public void Setup()
    {
        var bytes = new byte[InstructionCount];
        System.Array.Fill(bytes, (byte)OpCode.PUSH0);
        _script = new Script(bytes);
        _context = new ExecutionContext(_script, 0, new ReferenceCounter());
    }

    [IterationSetup]
    public void IterationSetup()
    {
        WarmInstructionCache();
        _context.InstructionPointer = 0;
    }

    [Benchmark]
    public Instruction? AdvanceWithMoveNext()
    {
        Instruction? last = null;
        while (true)
        {
            last = _context.CurrentInstruction ?? Instruction.RET;
            if (!_context.MoveNext()) break;
        }
        return last;
    }

    [Benchmark]
    public Instruction? AdvanceWithPointer()
    {
        Instruction? last = null;
        while (true)
        {
            var current = _context.CurrentInstruction;
            if (current is null) break;
            last = current;
            _context.InstructionPointer += current.Size;
        }
        return last;
    }

    private void WarmInstructionCache()
    {
        while (true)
        {
            var current = _context.CurrentInstruction;
            if (current is null) break;
            _context.InstructionPointer += current.Size;
        }
    }
}
