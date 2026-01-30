// Copyright (C) 2015-2026 The Neo Project.
//
// Benchmarks_StackOps.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;

namespace Neo.VM.Benchmarks.OpCodes;

[MemoryDiagnoser]
[CsvExporter]
[MarkdownExporterAttribute.GitHub]
[InvocationCount(1)]
public class Benchmarks_StackOps
{
    private const int RepeatCount = 1000;
    private const int MultipleCount = 50;
    private const int MultipleIterations = RepeatCount * MultipleCount;

    // Keep description strings in sync with RepeatCount/MultipleCount.
    private const string SwapShallowDescription = "SWAP_Shallow_2x1000";
    private const string SwapDeepDescription = "SWAP_Deep_100x1000";
    private const string SwapMultipleDescription = "SWAP_Multiple_50x1000";
    private const string RotShallowDescription = "ROT_Shallow_3x1000";
    private const string RotDeepDescription = "ROT_Deep_100x1000";
    private const string RotMultipleDescription = "ROT_Multiple_50x1000";

    private BenchmarkEngine _engine = null!;

    private byte[] _swapShallowScript = [];
    private byte[] _swapDeepScript = [];
    private byte[] _swapMultipleScript = [];
    private byte[] _rotShallowScript = [];
    private byte[] _rotDeepScript = [];
    private byte[] _rotMultipleScript = [];

    [GlobalSetup]
    public void GlobalSetup()
    {
        _swapShallowScript = BuildScript(sb =>
        {
            sb.EmitPush(1);
            sb.EmitPush(2);
            for (int i = 0; i < RepeatCount; i++)
                sb.Emit(OpCode.SWAP);
        });
        _swapDeepScript = BuildScript(sb =>
        {
            for (int i = 0; i < 100; i++)
                sb.EmitPush(i);
            for (int i = 0; i < RepeatCount; i++)
                sb.Emit(OpCode.SWAP);
        });
        _swapMultipleScript = BuildScript(sb =>
        {
            sb.EmitPush(1);
            sb.EmitPush(2);
            for (int i = 0; i < MultipleIterations; i++)
                sb.Emit(OpCode.SWAP);
        });

        _rotShallowScript = BuildScript(sb =>
        {
            sb.EmitPush(1);
            sb.EmitPush(2);
            sb.EmitPush(3);
            for (int i = 0; i < RepeatCount; i++)
                sb.Emit(OpCode.ROT);
        });
        _rotDeepScript = BuildScript(sb =>
        {
            for (int i = 0; i < 100; i++)
                sb.EmitPush(i);
            for (int i = 0; i < RepeatCount; i++)
                sb.Emit(OpCode.ROT);
        });
        _rotMultipleScript = BuildScript(sb =>
        {
            sb.EmitPush(1);
            sb.EmitPush(2);
            sb.EmitPush(3);
            for (int i = 0; i < MultipleIterations; i++)
                sb.Emit(OpCode.ROT);
        });
    }

    private static byte[] BuildScript(Action<ScriptBuilder> build)
    {
        var sb = new ScriptBuilder();
        build(sb);
        return sb.ToArray();
    }

    private void SetupEngine(byte[] script)
    {
        _engine = new BenchmarkEngine();
        _engine.LoadScript(script);
    }

    #region SWAP Benchmarks

    [IterationSetup(Target = nameof(Bench_SWAP_Shallow))]
    public void Setup_SWAP_Shallow() => SetupEngine(_swapShallowScript);

    [Benchmark(Description = SwapShallowDescription)]
    public void Bench_SWAP_Shallow()
    {
        _engine.ExecuteBenchmark();
    }

    [IterationSetup(Target = nameof(Bench_SWAP_Deep))]
    public void Setup_SWAP_Deep() => SetupEngine(_swapDeepScript);

    [Benchmark(Description = SwapDeepDescription)]
    public void Bench_SWAP_Deep()
    {
        _engine.ExecuteBenchmark();
    }

    [IterationSetup(Target = nameof(Bench_SWAP_Multiple))]
    public void Setup_SWAP_Multiple() => SetupEngine(_swapMultipleScript);

    [Benchmark(Description = SwapMultipleDescription)]
    public void Bench_SWAP_Multiple()
    {
        _engine.ExecuteBenchmark();
    }

    #endregion

    #region ROT Benchmarks

    [IterationSetup(Target = nameof(Bench_ROT_Shallow))]
    public void Setup_ROT_Shallow() => SetupEngine(_rotShallowScript);

    [Benchmark(Description = RotShallowDescription)]
    public void Bench_ROT_Shallow()
    {
        _engine.ExecuteBenchmark();
    }

    [IterationSetup(Target = nameof(Bench_ROT_Deep))]
    public void Setup_ROT_Deep() => SetupEngine(_rotDeepScript);

    [Benchmark(Description = RotDeepDescription)]
    public void Bench_ROT_Deep()
    {
        _engine.ExecuteBenchmark();
    }

    [IterationSetup(Target = nameof(Bench_ROT_Multiple))]
    public void Setup_ROT_Multiple() => SetupEngine(_rotMultipleScript);

    [Benchmark(Description = RotMultipleDescription)]
    public void Bench_ROT_Multiple()
    {
        _engine.ExecuteBenchmark();
    }

    #endregion
}
