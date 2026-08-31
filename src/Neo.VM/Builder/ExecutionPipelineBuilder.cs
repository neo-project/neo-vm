// Copyright (C) 2015-2026 The Neo Project.
//
// ExecutionPipelineBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Middleware;
using Neo.VM.Pipeline;
using System;
using System.Collections.Generic;

namespace Neo.VM.Builder;

/// <summary>
/// Fluent builder for constructing an <see cref="ExecutionPipeline"/>.
/// Middleware is executed in the order it is registered (first registered = first to run).
/// </summary>
public sealed class ExecutionPipelineBuilder
{
    private readonly List<IEngineMiddleware> _middleware = [];

    private ExecutionPipelineBuilder() { }

    /// <summary>
    /// Creates a new <see cref="ExecutionPipelineBuilder"/> instance.
    /// </summary>
    /// <returns>A new builder.</returns>
    public static ExecutionPipelineBuilder Create()
        => new();

    /// <summary>
    /// Registers a middleware instance.
    /// </summary>
    /// <param name="middleware">The middleware to register.</param>
    /// <returns>This builder for chaining.</returns>
    public ExecutionPipelineBuilder Use(IEngineMiddleware middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        _middleware.Add(middleware);
        return this;
    }

    /// <summary>
    /// Registers a middleware by type (requires a parameterless constructor).
    /// </summary>
    /// <typeparam name="TMiddleware">The middleware type to instantiate and register.</typeparam>
    /// <returns>This builder for chaining.</returns>
    public ExecutionPipelineBuilder UseMiddleware<TMiddleware>()
        where TMiddleware : IEngineMiddleware, new()
    {
        return Use(new TMiddleware());
    }

    /// <summary>
    /// Registers multiple middleware instances at once.
    /// </summary>
    /// <param name="middlewares">The middleware instances to register, in execution order.</param>
    /// <returns>This builder for chaining.</returns>
    public ExecutionPipelineBuilder Use(IEnumerable<IEngineMiddleware> middlewares)
    {
        foreach (var middleware in middlewares)
            Use(middleware);

        return this;
    }

    /// <summary>
    /// Builds the pipeline with separate chains for each hook.
    /// </summary>
    /// <returns>A pipeline ready for use by <see cref="ExecutionEngine"/>.</returns>
    public ExecutionPipeline Build()
    {
        var preExecution = BuildExecutionChain(static mw => mw.PreExecution);
        var postExecution = BuildExecutionChain(static mw => mw.PostExecution);
        var preExecute = BuildExecuteChain(static mw => mw.PreExecute);
        var postExecute = BuildExecuteChain(static mw => mw.PostExecute);

        return new
        (
            preExecution,
            postExecution,
            preExecute,
            postExecute
        );
    }

    private ExecuteDelegate BuildExecuteChain(Func<IEngineMiddleware, Action<ExecutionContext?, ExecuteDelegate>> selector)
    {
        ExecuteDelegate app = static _ => { };

        for (var i = _middleware.Count - 1; i >= 0; i--)
        {
            var middleware = _middleware[i];
            var current = selector(middleware);
            var next = app;

            app = context => current(context, next);
        }

        return app;
    }

    private ExecutionDelegate BuildExecutionChain(Func<IEngineMiddleware, Action<ExecutionDelegate>> selector)
    {
        ExecutionDelegate app = static () => { };

        for (var i = _middleware.Count - 1; i >= 0; i--)
        {
            var middleware = _middleware[i];
            var current = selector(middleware);
            var next = app;

            app = () => current(next);
        }

        return app;
    }
}
