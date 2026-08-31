// Copyright (C) 2015-2026 The Neo Project.
//
// IEngineMiddleware.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Middleware;

/// <summary>
/// Represents a middleware handler invoked around individual opcode execution.
/// </summary>
/// <param name="context">The current execution context, or <see langword="null"/> when none is active.</param>
public delegate void ExecuteDelegate(ExecutionContext? context);

/// <summary>
/// Represents a middleware handler invoked around the entire VM run.
/// </summary>
public delegate void ExecutionDelegate();

/// <summary>
/// Defines middleware hooks that participate in the <see cref="Pipeline.ExecutionPipeline"/>.
/// Implementations must invoke the provided next delegate to continue the chain.
/// </summary>
public interface IEngineMiddleware
{
    /// <summary>
    /// Called when the VM starts execution.
    /// </summary>
    /// <param name="next">The next handler in the pre-execution chain.</param>
    void PreExecution(ExecutionDelegate next)
        => next();

    /// <summary>
    /// Called when the VM finishes execution (<see cref="VMState.HALT"/>, <see cref="VMState.FAULT"/>, etc.).
    /// </summary>
    /// <param name="next">The next handler in the post-execution chain.</param>
    void PostExecution(ExecutionDelegate next)
        => next();

    /// <summary>
    /// Called before each opcode is executed.
    /// </summary>
    /// <param name="context">The current execution context, if any.</param>
    /// <param name="next">The next handler in the pre-execute chain.</param>
    void PreExecute(ExecutionContext? context, ExecuteDelegate next)
        => next(context);

    /// <summary>
    /// Called after each opcode is executed.
    /// </summary>
    /// <param name="context">The current execution context, if any.</param>
    /// <param name="next">The next handler in the post-execute chain.</param>
    void PostExecute(ExecutionContext? context, ExecuteDelegate next)
        => next(context);
}
