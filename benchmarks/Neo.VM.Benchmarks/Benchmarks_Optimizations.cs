// Copyright (C) 2015-2026 The Neo Project.
//
// Benchmarks_Optimizations.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.VM.Benchmarks.Builders;
using Neo.VM.Types;
using BuilderInstruction = Neo.VM.Benchmarks.Builders.Instruction;
using SysArray = System.Array;

namespace Neo.VM.Benchmarks.OpCodes;

[MemoryDiagnoser]
[InvocationCount(1)]
[IterationCount(10)]
[WarmupCount(3)]
public class Benchmarks_Optimizations
{
    private const int LoopIterations = 100000;
    private const int SmallArraySize = 8;
    private const int MediumArraySize = 32;
    private const int LargeArraySize = 128;

    private byte[] _newArraySmallScript = SysArray.Empty<byte>();
    private byte[] _newArrayMediumScript = SysArray.Empty<byte>();
    private byte[] _newArrayLargeScript = SysArray.Empty<byte>();
    private byte[] _newStructScript = SysArray.Empty<byte>();
    private byte[] _initSlotScript = SysArray.Empty<byte>();

    [GlobalSetup]
    public void Setup()
    {
        _newArraySmallScript = BuildLoop(OpCode.NEWARRAY, SmallArraySize);
        _newArrayMediumScript = BuildLoop(OpCode.NEWARRAY, MediumArraySize);
        _newArrayLargeScript = BuildLoop(OpCode.NEWARRAY, LargeArraySize);
        _newStructScript = BuildLoop(OpCode.NEWSTRUCT, SmallArraySize);
        _initSlotScript = BuildInitSlotScript(SmallArraySize);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = LoopIterations)]
    public void NewArray_Small()
    {
        RunScript(_newArraySmallScript);
    }

    [Benchmark(OperationsPerInvoke = LoopIterations)]
    public void NewArray_Medium()
    {
        RunScript(_newArrayMediumScript);
    }

    [Benchmark(OperationsPerInvoke = LoopIterations)]
    public void NewArray_Large()
    {
        RunScript(_newArrayLargeScript);
    }

    [Benchmark(OperationsPerInvoke = LoopIterations)]
    public void NewStruct()
    {
        RunScript(_newStructScript);
    }

    [Benchmark(OperationsPerInvoke = LoopIterations)]
    public void InitSlot()
    {
        RunScript(_initSlotScript);
    }

    // Memory allocation benchmarks
    [Benchmark]
    public void CreateManyArrays_UsingPool()
    {
        using var engine = new SimpleBenchmarkEngine();
        for (int i = 0; i < 10000; i++)
        {
            var arr = new Neo.VM.Types.Array(engine.ReferenceCounter, StackItem.Null, SmallArraySize, skipReferenceCounting: false);
        }
    }

    private static void RunScript(byte[] script)
    {
        using var engine = new SimpleBenchmarkEngine();
        engine.LoadScript(script);
        while (engine.State != VMState.HALT && engine.State != VMState.FAULT)
        {
            engine.ExecuteNext();
        }
    }

    private static byte[] BuildLoop(OpCode opCode, int size)
    {
        var builder = new InstructionBuilder();
        builder.Push(LoopIterations);
        builder.AddInstruction(OpCode.STLOC0);

        var loopStart = new JumpTarget
        {
            _instruction = builder.AddInstruction(OpCode.NOP)
        };

        builder.Push(size);
        builder.AddInstruction(opCode);
        builder.AddInstruction(OpCode.DROP);

        builder.AddInstruction(OpCode.LDLOC0);
        builder.AddInstruction(OpCode.DEC);
        builder.AddInstruction(OpCode.STLOC0);
        builder.AddInstruction(OpCode.LDLOC0);
        builder.Jump(OpCode.JMPIF, loopStart);
        builder.Ret();
        return builder.ToArray();
    }

    private static byte[] BuildInitSlotScript(int slotSize)
    {
        var builder = new InstructionBuilder();
        // Push some items on stack
        for (int i = 0; i < slotSize; i++)
        {
            builder.Push(i);
        }
        // InitSlot with arguments = slotSize, locals = 0
        builder.AddInstruction(new BuilderInstruction
        {
            _opCode = OpCode.INITSLOT,
            _operand = [0, (byte)slotSize]
        });
        builder.Ret();
        return builder.ToArray();
    }

    private sealed class SimpleBenchmarkEngine : ExecutionEngine
    {
        public SimpleBenchmarkEngine()
            : base(null, new ReferenceCounter(), ExecutionEngineLimits.Default)
        {
        }
    }
}
