// Copyright (C) 2015-2026 The Neo Project.
//
// ExecuteLoggerMiddleware.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Neo.VM.Extensions;
using System;

namespace Neo.VM.Middleware;

/// <summary>
/// Middleware that logs VM startup, per-opcode execution (at trace level), and completion.
/// </summary>
public sealed class ExecuteLoggerMiddleware : IEngineMiddleware
{
    private readonly ExecutionEngine _engine;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteLoggerMiddleware"/> class.
    /// Attach it with <see cref="ExecutionEngine.Use"/>.
    /// </summary>
    /// <param name="engine">The execution engine to log.</param>
    /// <param name="logger">Logger that receives execution messages. Uses a no-op logger when omitted.</param>
    public ExecuteLoggerMiddleware(ExecutionEngine engine, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(engine);
        _engine = engine;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public void PreExecution(ExecutionDelegate next)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogExecuteMessage(LogLevel.Information, $"VM execution starting | State: {_engine.State}");
            if (_engine.State == VMState.FAULT && _engine.FaultException is not null)
                _logger.LogFaultMessage(LogLevel.Critical, _engine.FaultException, _engine.FaultException.Message);
        }

        next();
    }

    /// <inheritdoc />
    public void PreExecute(ExecutionContext? context, ExecuteDelegate next)
    {
        if (context is not null && _logger.IsEnabled(LogLevel.Trace))
        {
            var instruction = context.CurrentInstruction;
            var opcode = instruction?.OpCode.ToString() ?? "RET";
            var operand = instruction is { Operand.Length: > 0 }
                ? Convert.ToHexString(instruction.Operand.Span)
                : string.Empty;
            var suffix = operand.Length == 0 ? string.Empty : $" {operand}";
            _logger.LogExecuteMessage(
                LogLevel.Trace,
                $"Execute opcode | IP: {context.InstructionPointer:X04} | {opcode}{suffix}");
        }

        next(context);
    }

    /// <inheritdoc />
    public void PostExecute(ExecutionContext? context, ExecuteDelegate next)
        => next(context);

    /// <inheritdoc />
    public void PostExecution(ExecutionDelegate next)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogExecuteMessage(LogLevel.Information, $"VM execution finished | State: {_engine.State}");
            if (_engine.State == VMState.FAULT && _engine.FaultException is not null)
                _logger.LogFaultMessage(LogLevel.Critical, _engine.FaultException, _engine.FaultException.Message);
        }

        next();
    }
}
