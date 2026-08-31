// Copyright (C) 2015-2026 The Neo Project.
//
// ExecutionEngineBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Pipeline;
using System;

namespace Neo.VM.Builder;

/// <summary>
/// Fluent builder for constructing an <see cref="ExecutionEngine"/>.
/// </summary>
public sealed class ExecutionEngineBuilder
{
    private JumpTable? _jumpTable;
    private ExecutionEngineLimits? _limits;
    private IReferenceCounter? _referenceCounter;
    private ExecutionPipeline? _pipeline;

    private ExecutionEngineBuilder() { }

    /// <summary>
    /// Creates a new <see cref="ExecutionEngineBuilder"/> instance.
    /// </summary>
    /// <returns>A new builder.</returns>
    public static ExecutionEngineBuilder Create()
        => new();

    /// <summary>
    /// Configures the middleware pipeline used during execution.
    /// </summary>
    /// <param name="pipeline">The pipeline to use.</param>
    /// <returns>This builder for chaining.</returns>
    public ExecutionEngineBuilder UsePipeline(ExecutionPipeline pipeline)
    {
        _pipeline = pipeline;
        return this;
    }

    /// <summary>
    /// Configures the middleware pipeline via a nested <see cref="ExecutionPipelineBuilder"/>.
    /// </summary>
    /// <param name="config">An action that registers middleware on the pipeline builder.</param>
    /// <returns>This builder for chaining.</returns>
    public ExecutionEngineBuilder UsePipeline(Action<ExecutionPipelineBuilder> config)
    {
        var pb = ExecutionPipelineBuilder.Create();
        config(pb);
        return UsePipeline(pb.Build());
    }

    /// <summary>
    /// Sets the execution limits for the engine.
    /// </summary>
    /// <param name="limits">The limits to apply.</param>
    /// <returns>This builder for chaining.</returns>
    public ExecutionEngineBuilder UseLimits(ExecutionEngineLimits limits)
    {
        _limits = limits;
        return this;
    }

    /// <summary>
    /// Sets the opcode jump table used by the engine.
    /// </summary>
    /// <param name="jumpTable">The jump table of opcode handlers.</param>
    /// <returns>This builder for chaining.</returns>
    public ExecutionEngineBuilder UseJumpTable(JumpTable jumpTable)
    {
        _jumpTable = jumpTable;
        return this;
    }

    /// <summary>
    /// Sets the reference counter used by the engine.
    /// </summary>
    /// <param name="referenceCounter">The reference counter.</param>
    /// <returns>This builder for chaining.</returns>
    public ExecutionEngineBuilder UseReferenceCounter(IReferenceCounter referenceCounter)
    {
        _referenceCounter = referenceCounter;
        return this;
    }

    /// <summary>
    /// Builds an <see cref="ExecutionEngine"/> from the configured options.
    /// Unspecified options use engine defaults.
    /// </summary>
    /// <returns>A new engine instance.</returns>
    public ExecutionEngine Build()
    {
        var limits = _limits ?? ExecutionEngineLimits.Default;
        var referenceCounter = _referenceCounter ?? new ReferenceCounter(limits);
        return new ExecutionEngine(_jumpTable, referenceCounter, limits, _pipeline);
    }
}
