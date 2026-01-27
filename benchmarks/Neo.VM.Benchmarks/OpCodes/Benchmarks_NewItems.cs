// Copyright (C) 2015-2026 The Neo Project.
//
// Benchmarks_NewItems.cs file belongs to the neo project and is free
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
public class Benchmarks_NewItems
{
    private const int LoopIterations = 1048576;
    private const int ArraySize = 16;
    private const int BufferSize = 32;

    private byte[] _baselineScript = SysArray.Empty<byte>();
    private byte[] _newArrayScript = SysArray.Empty<byte>();
    private byte[] _newStructScript = SysArray.Empty<byte>();
    private byte[] _newArrayTScript = SysArray.Empty<byte>();
    private byte[] _newBufferScript = SysArray.Empty<byte>();

    [GlobalSetup]
    public void Setup()
    {
        _baselineScript = BuildLoop();
        _newArrayScript = BuildLoop(OpCode.NEWARRAY, ArraySize);
        _newStructScript = BuildLoop(OpCode.NEWSTRUCT, ArraySize);
        _newArrayTScript = BuildLoop(OpCode.NEWARRAY_T, ArraySize, StackItemType.Integer);
        _newBufferScript = BuildLoop(OpCode.NEWBUFFER, BufferSize);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = LoopIterations)]
    public void Baseline()
    {
        RunCurrent(_baselineScript);
    }

    [Benchmark(OperationsPerInvoke = LoopIterations)]
    public void NewArray()
    {
        RunCurrent(_newArrayScript);
    }

    [Benchmark(OperationsPerInvoke = LoopIterations)]
    public void LegacyNewArray()
    {
        RunLegacy(_newArrayScript);
    }

    [Benchmark(OperationsPerInvoke = LoopIterations)]
    public void NewStruct()
    {
        RunCurrent(_newStructScript);
    }

    [Benchmark(OperationsPerInvoke = LoopIterations)]
    public void LegacyNewStruct()
    {
        RunLegacy(_newStructScript);
    }

    [Benchmark(OperationsPerInvoke = LoopIterations)]
    public void NewArrayT()
    {
        RunCurrent(_newArrayTScript);
    }

    [Benchmark(OperationsPerInvoke = LoopIterations)]
    public void LegacyNewArrayT()
    {
        RunLegacy(_newArrayTScript);
    }

    [Benchmark(OperationsPerInvoke = LoopIterations)]
    public void NewBuffer()
    {
        RunCurrent(_newBufferScript);
    }

    private static void RunCurrent(byte[] script)
    {
        using var engine = new SimpleBenchmarkEngine();
        RunWithEngine(script, engine);
    }

    private static void RunLegacy(byte[] script)
    {
        using var engine = new SimpleBenchmarkEngine(new LegacyJumpTable());
        RunWithEngine(script, engine);
    }

    private static void RunWithEngine(byte[] script, SimpleBenchmarkEngine engine)
    {
        engine.LoadScript(script);
        engine.ExecuteBenchmark();
    }

    private static byte[] BuildLoop(OpCode? createOp = null, int size = 0, StackItemType arrayType = StackItemType.Any)
    {
        var builder = new InstructionBuilder();
        builder.AddInstruction(new BuilderInstruction
        {
            _opCode = OpCode.INITSLOT,
            _operand = [1, 0]
        });
        builder.Push(LoopIterations);
        builder.AddInstruction(OpCode.STLOC0);

        var loopStart = new JumpTarget
        {
            _instruction = builder.AddInstruction(OpCode.NOP)
        };

        if (createOp.HasValue)
        {
            builder.Push(size);
            if (createOp.Value == OpCode.NEWARRAY_T)
            {
                builder.AddInstruction(new BuilderInstruction
                {
                    _opCode = OpCode.NEWARRAY_T,
                    _operand = [(byte)arrayType]
                });
            }
            else
            {
                builder.AddInstruction(createOp.Value);
            }
            builder.AddInstruction(OpCode.DROP);
        }

        builder.AddInstruction(OpCode.LDLOC0);
        builder.AddInstruction(OpCode.DEC);
        builder.AddInstruction(OpCode.STLOC0);
        builder.AddInstruction(OpCode.LDLOC0);
        builder.Jump(OpCode.JMPIF, loopStart);
        builder.Ret();
        return builder.ToArray();
    }

    private sealed class SimpleBenchmarkEngine : ExecutionEngine
    {
        public SimpleBenchmarkEngine(JumpTable? jumpTable = null)
            : base(jumpTable, new ReferenceCounter(), ExecutionEngineLimits.Default)
        {
        }

        public void ExecuteBenchmark()
        {
            while (State != VMState.HALT && State != VMState.FAULT)
            {
                ExecuteNext();
            }
        }
    }

    private sealed class LegacyJumpTable : JumpTable
    {
        public override void NewArray(ExecutionEngine engine, Neo.VM.Instruction instruction)
        {
            var n = (int)engine.Pop().GetInteger();
            if (n < 0 || n > engine.Limits.MaxStackSize)
                throw new InvalidOperationException($"The array size is out of valid range, {n}/[0, {engine.Limits.MaxStackSize}].");
            var nullArray = new StackItem[n];
            System.Array.Fill(nullArray, StackItem.Null);
            engine.Push(new Neo.VM.Types.Array(engine.ReferenceCounter, nullArray));
        }

        public override void NewArray_T(ExecutionEngine engine, Neo.VM.Instruction instruction)
        {
            var n = (int)engine.Pop().GetInteger();
            if (n < 0 || n > engine.Limits.MaxStackSize)
                throw new InvalidOperationException($"The array size is out of valid range, {n}/[0, {engine.Limits.MaxStackSize}].");

            var type = (StackItemType)instruction.TokenU8;
#if NET5_0_OR_GREATER
            if (!Enum.IsDefined(type))
#else
            if (!Enum.IsDefined(typeof(StackItemType), type))
#endif
                throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {instruction.TokenU8}");

            var item = instruction.TokenU8 switch
            {
                (byte)StackItemType.Boolean => StackItem.False,
                (byte)StackItemType.Integer => Integer.Zero,
                (byte)StackItemType.ByteString => ByteString.Empty,
                _ => StackItem.Null
            };
            var itemArray = new StackItem[n];
            System.Array.Fill(itemArray, item);
            engine.Push(new Neo.VM.Types.Array(engine.ReferenceCounter, itemArray));
        }

        public override void NewStruct(ExecutionEngine engine, Neo.VM.Instruction instruction)
        {
            var n = (int)engine.Pop().GetInteger();
            if (n < 0 || n > engine.Limits.MaxStackSize)
                throw new InvalidOperationException($"The struct size is out of valid range, {n}/[0, {engine.Limits.MaxStackSize}].");

            var nullArray = new StackItem[n];
            System.Array.Fill(nullArray, StackItem.Null);
            engine.Push(new Struct(engine.ReferenceCounter, nullArray));
        }
    }
}
