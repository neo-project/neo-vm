// Copyright (C) 2015-2026 The Neo Project.
//
// ExecutionPipeline.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Middleware;

namespace Neo.VM.Pipeline;

/// <summary>
/// Represents the built middleware pipeline for <see cref="ExecutionEngine"/>.
/// Contains four separate, pre-built delegate chains for the different execution hooks.
/// </summary>
public sealed class ExecutionPipeline
{
    /// <summary>
    /// Delegate chain executed before the entire VM execution starts.
    /// </summary>
    public ExecutionDelegate PreExecution { get; }

    /// <summary>
    /// Delegate chain executed after the entire VM execution finishes (HALT or FAULT).
    /// </summary>
    public ExecutionDelegate PostExecution { get; }

    /// <summary>
    /// Delegate chain executed before every individual opcode.
    /// </summary>
    public ExecuteDelegate PreExecute { get; }

    /// <summary>
    /// Delegate chain executed after every individual opcode.
    /// </summary>
    public ExecuteDelegate PostExecute { get; }

    internal ExecutionPipeline(
        ExecutionDelegate preExecution,
        ExecutionDelegate postExecution,
        ExecuteDelegate preExecute,
        ExecuteDelegate postExecute)
    {
        PreExecution = preExecution;
        PostExecution = postExecution;
        PreExecute = preExecute;
        PostExecute = postExecute;
    }

    /// <summary>
    /// Returns an empty pipeline (no middleware registered).
    /// </summary>
    public static ExecutionPipeline Empty { get; } = new
    (
        static () => { },
        static () => { },
        static _ => { },
        static _ => { }
    );
}
