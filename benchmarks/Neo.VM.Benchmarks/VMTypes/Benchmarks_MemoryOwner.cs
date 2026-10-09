// Copyright (C) 2015-2026 The Neo Project.
//
// Benchmarks_MemoryOwner.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Neo.VM.Types;
using System.Buffers;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.VM.Benchmarks.VMTypes;

/// <summary>
/// Before = master (ByteString aliases caller memory; Buffer uses ArrayPool).
/// After = this PR (IMemoryOwner from MemoryPool.Shared).
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class Benchmarks_MemoryOwner
{
    [Params(32, 256, 4096)]
    public int Size;

    private byte[] _data = null!;
    private byte[] _newBufferScript = null!;
    private byte[] _pushDataScript = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = new byte[Size];
        _data.AsSpan().Fill(0x5A);

        using (var sb = new ScriptBuilder())
        {
            sb.EmitPush(Size);
            sb.Emit(OpCode.NEWBUFFER);
            sb.Emit(OpCode.DROP);
            sb.Emit(OpCode.RET);
            _newBufferScript = sb.ToArray();
        }

        using (var sb = new ScriptBuilder())
        {
            sb.EmitPush(_data);
            sb.Emit(OpCode.DROP);
            sb.Emit(OpCode.RET);
            _pushDataScript = sb.ToArray();
        }
    }

    [Benchmark(Baseline = true, Description = "ByteString alias (master)")]
    [BenchmarkCategory("ByteString")]
    public int Before_ByteString_Alias()
    {
        ReadOnlyMemory<byte> memory = _data;
        return memory.Span[Size - 1];
    }

    [Benchmark(Description = "ByteString copy into MemoryPool (PR)")]
    [BenchmarkCategory("ByteString")]
    public int After_ByteString_Owner()
    {
        var item = new ByteString(_data);
        var value = item.GetSpan()[Size - 1];
        item.Cleanup();
        return value;
    }

    [Benchmark(Baseline = true, Description = "Buffer ArrayPool rent+return (master)")]
    [BenchmarkCategory("Buffer")]
    public int Before_Buffer_ArrayPool()
    {
        var rented = ArrayPool<byte>.Shared.Rent(Size);
        var inner = new Memory<byte>(rented, 0, Size);
        inner.Span.Clear();
        var value = inner.Span[Size - 1];
        ArrayPool<byte>.Shared.Return(rented, clearArray: false);
        return value;
    }

    [Benchmark(Description = "Buffer MemoryPool IMemoryOwner (PR)")]
    [BenchmarkCategory("Buffer")]
    public int After_Buffer_Owner()
    {
        var buffer = new Buffer(Size);
        var value = buffer.InnerBuffer.Span[Size - 1];
        buffer.Cleanup();
        return value;
    }

    [Benchmark(Baseline = true, Description = "Buffer copy from span via ArrayPool (master)")]
    [BenchmarkCategory("BufferCopy")]
    public int Before_Buffer_CopyFromSpan()
    {
        var rented = ArrayPool<byte>.Shared.Rent(Size);
        var inner = new Memory<byte>(rented, 0, Size);
        _data.CopyTo(inner.Span);
        var value = inner.Span[Size - 1];
        ArrayPool<byte>.Shared.Return(rented, clearArray: false);
        return value;
    }

    [Benchmark(Description = "Buffer copy from span via MemoryPool (PR)")]
    [BenchmarkCategory("BufferCopy")]
    public int After_Buffer_CopyFromSpan()
    {
        var buffer = new Buffer(_data);
        var value = buffer.InnerBuffer.Span[Size - 1];
        buffer.Cleanup();
        return value;
    }

    [Benchmark(Description = "Opcode NEWBUFFER + DROP (PR)")]
    [BenchmarkCategory("Opcode")]
    public VMState After_Opcode_NewBuffer()
    {
        using var engine = new ExecutionEngine();
        engine.LoadScript(_newBufferScript);
        return engine.Execute();
    }

    [Benchmark(Description = "Opcode PUSHDATA + DROP (PR)")]
    [BenchmarkCategory("Opcode")]
    public VMState After_Opcode_PushData()
    {
        using var engine = new ExecutionEngine();
        engine.LoadScript(_pushDataScript);
        return engine.Execute();
    }
}

// Run (from repo root):
//   $env:NEO_VM_BENCHMARK='1'
//   dotnet run -c Release --project benchmarks/Neo.VM.Benchmarks -- --filter *MemoryOwner* --job short
//
// BenchmarkDotNet v0.15.8, Windows 11, Intel Core Ultra 7 255U, .NET 10.0.11, Job=ShortRun
//
// | Method                                      | Category    | Size | Mean      | Ratio | Allocated |
// |---------------------------------------------|-------------|-----:|----------:|------:|----------:|
// | Buffer ArrayPool rent+return (master)       | Buffer      |   32 | 16.67 ns  |  1.00 |         - |
// | Buffer MemoryPool IMemoryOwner (PR)         | Buffer      |   32 | 22.05 ns  |  1.33 |      24 B |
// | Buffer ArrayPool rent+return (master)       | Buffer      |  256 | 20.62 ns  |  1.00 |         - |
// | Buffer MemoryPool IMemoryOwner (PR)         | Buffer      |  256 | 24.62 ns  |  1.20 |      24 B |
// | Buffer ArrayPool rent+return (master)       | Buffer      | 4096 | 54.54 ns  |  1.00 |         - |
// | Buffer MemoryPool IMemoryOwner (PR)         | Buffer      | 4096 | 34.38 ns  |  0.63 |      24 B |
// | Buffer copy from span ArrayPool (master)    | BufferCopy  |   32 | 10.53 ns  |  1.00 |         - |
// | Buffer copy from span MemoryPool (PR)       | BufferCopy  |   32 | 14.74 ns  |  1.40 |      24 B |
// | Buffer copy from span ArrayPool (master)    | BufferCopy  |  256 | 14.25 ns  |  1.00 |         - |
// | Buffer copy from span MemoryPool (PR)       | BufferCopy  |  256 | 17.78 ns  |  1.25 |      24 B |
// | Buffer copy from span ArrayPool (master)    | BufferCopy  | 4096 | 36.15 ns  |  1.00 |         - |
// | Buffer copy from span MemoryPool (PR)       | BufferCopy  | 4096 | 45.71 ns  |  1.26 |      24 B |
// | ByteString alias (master)                   | ByteString  |   32 |  0.70 ns  |  1.00 |         - |
// | ByteString copy into MemoryPool (PR)        | ByteString  |   32 | 20.96 ns  | 30.22 |      72 B |
// | ByteString alias (master)                   | ByteString  |  256 |  0.60 ns  |  1.00 |         - |
// | ByteString copy into MemoryPool (PR)        | ByteString  |  256 | 24.37 ns  | 40.48 |      72 B |
// | ByteString alias (master)                   | ByteString  | 4096 |  0.78 ns  |  1.00 |         - |
// | ByteString copy into MemoryPool (PR)        | ByteString  | 4096 | 47.99 ns  | 62.45 |      72 B |
// | Opcode NEWBUFFER + DROP (PR)                | Opcode      |   32 | 310.5 ns  |     - |    1424 B |
// | Opcode PUSHDATA + DROP (PR)                 | Opcode      |   32 | 305.7 ns  |     - |    1096 B |
// | Opcode NEWBUFFER + DROP (PR)                | Opcode      |  256 | 522.2 ns  |     - |    1648 B |
// | Opcode PUSHDATA + DROP (PR)                 | Opcode      |  256 | 236.0 ns  |     - |    1320 B |
// | Opcode NEWBUFFER + DROP (PR)                | Opcode      | 4096 | 481.9 ns  |     - |    5488 B |
// | Opcode PUSHDATA + DROP (PR)                 | Opcode      | 4096 | 392.1 ns  |     - |    5160 B |
