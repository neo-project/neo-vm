// Copyright (C) 2015-2026 The Neo Project.
//
// DebuggerMiddleware.cs file belongs to the neo project and is free
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
using System.Collections.Generic;
using System.Linq;

namespace Neo.VM.Middleware;

/// <summary>
/// Middleware that supports script-offset breakpoints and single-step execution.
/// When a breakpoint is hit, the engine enters <see cref="VMState.BREAK"/>.
/// </summary>
public class DebuggerMiddleware : IEngineMiddleware
{
    private readonly ExecutionEngine _engine;
    private readonly ILogger _logger;
    private readonly Dictionary<Script, HashSet<uint>> _breakPoints = [];
    private int _lastStepPosition = -1;

    /// <summary>
    /// Raised when a breakpoint or step stop occurs.
    /// </summary>
    public event EventHandler<DebuggerEventArgs>? OnBreakpoint;

    /// <summary>
    /// Gets or sets whether the debugger pauses after each distinct instruction pointer.
    /// </summary>
    public bool StepMode { get; set; }

    private ExecutionEngine Engine
        => _engine;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebuggerMiddleware"/> class
    /// and registers it on <paramref name="engine"/>.
    /// </summary>
    /// <param name="engine">The <see cref="ExecutionEngine"/> to attach the debugger.</param>
    /// <param name="logger">Optional logger for debug messages. Uses a no-op logger when omitted.</param>
    public DebuggerMiddleware(ExecutionEngine engine, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(engine);
        _engine = engine;
        _logger = logger ?? NullLogger.Instance;
        engine.Use(this);
    }

    /// <summary>
    /// Registers a breakpoint at the specified position of the specified script.
    /// The VM will break the execution when it reaches the breakpoint.
    /// </summary>
    /// <param name="script">The script to add the breakpoint.</param>
    /// <param name="position">The position of the breakpoint in the script.</param>
    public void AddBreakPoint(Script script, uint position)
    {
        if (!_breakPoints.TryGetValue(script, out var hashset))
        {
            hashset = [];
            _breakPoints.Add(script, hashset);
        }
        hashset.Add(position);
    }

    /// <summary>
    /// Registers a breakpoint at the specified offset of the current script.
    /// </summary>
    /// <param name="scriptOffset">The instruction pointer value at which to break.</param>
    public void AddBreakpoint(int scriptOffset)
    {
        var context = Engine.CurrentContext ?? throw new InvalidOperationException("No execution context is loaded.");
        AddBreakPoint(context.Script, (uint)scriptOffset);
    }

    /// <summary>
    /// Resumes execution by clearing the <see cref="VMState.BREAK"/> state.
    /// </summary>
    public void Continue()
        => Engine.State = VMState.NONE;

    /// <summary>
    /// Start or continue execution of the VM.
    /// </summary>
    /// <returns>Returns the state of the VM after the execution.</returns>
    public VMState Execute()
        => Engine.Execute();

    /// <summary>
    /// Removes the breakpoint at the specified position in the specified script.
    /// </summary>
    /// <param name="script">The script to remove the breakpoint.</param>
    /// <param name="position">The position of the breakpoint in the script.</param>
    /// <returns>
    /// <see langword="true"/> if the breakpoint is successfully found and removed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool RemoveBreakPoint(Script script, uint position)
    {
        if (!_breakPoints.TryGetValue(script, out var hashset)) return false;
        if (!hashset.Remove(position)) return false;
        if (hashset.Count == 0) _breakPoints.Remove(script);
        return true;
    }

    /// <summary>
    /// Execute the next instruction.
    /// If the instruction involves a call to a method,
    /// it steps into the method and breaks the execution on the first instruction of that method.
    /// </summary>
    /// <returns>The VM state after the instruction is executed.</returns>
    public VMState StepInto()
    {
        if (Engine.State == VMState.HALT || Engine.State == VMState.FAULT)
            return Engine.State;
        Engine.ExecuteNext();
        if (Engine.State == VMState.NONE)
            Engine.State = VMState.BREAK;
        return Engine.State;
    }

    /// <summary>
    /// Execute until the currently executed method is returned.
    /// </summary>
    /// <returns>The VM state after the currently executed method is returned.</returns>
    public VMState StepOut()
    {
        if (Engine.State == VMState.BREAK)
            Engine.State = VMState.NONE;
        int c = Engine.InvocationStack.Count;
        while (Engine.State == VMState.NONE && Engine.InvocationStack.Count >= c)
            Engine.ExecuteNext();
        if (Engine.State == VMState.NONE)
            Engine.State = VMState.BREAK;
        return Engine.State;
    }

    /// <summary>
    /// Execute the next instruction.
    /// If the instruction involves a call to a method, it does not step into the method (it steps over it instead).
    /// </summary>
    /// <returns>The VM state after the instruction is executed.</returns>
    public VMState StepOver()
    {
        if (Engine.State == VMState.HALT || Engine.State == VMState.FAULT)
            return Engine.State;
        Engine.State = VMState.NONE;
        int c = Engine.InvocationStack.Count;
        do
        {
            Engine.ExecuteNext();
        }
        while (Engine.State == VMState.NONE && Engine.InvocationStack.Count > c);
        if (Engine.State == VMState.NONE)
            Engine.State = VMState.BREAK;
        return Engine.State;
    }

    /// <inheritdoc />
    public void PreExecution(ExecutionDelegate next)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var message = string.Join(", ", _breakPoints.SelectMany(static p => p.Value).Select(static s => $"\"L{s:D4}\""));
            _logger.LogExecuteMessage(LogLevel.Debug, $"Starting debugger | Breakpoints: [{message}]");
        }

        next();
    }

    /// <inheritdoc />
    public void PostExecution(ExecutionDelegate next)
    {
        StepMode = false;
        next();
    }

    /// <inheritdoc />
    public void PreExecute(ExecutionContext? context, ExecuteDelegate next)
        => next(context);

    /// <inheritdoc />
    public void PostExecute(ExecutionContext? context, ExecuteDelegate next)
    {
        next(context);
        // Use the engine's current context: CALL/RET may have switched frames.
        CheckBreakPoints(Engine.CurrentContext);
    }

    private void CheckBreakPoints(ExecutionContext? context)
    {
        if (context is null) return;
        if (Engine.State != VMState.NONE) return;

        var position = context.InstructionPointer;

        if (StepMode && position != _lastStepPosition)
        {
            BreakAt(context, position);
            return;
        }

        if (_breakPoints.Count == 0) return;
        if (_breakPoints.TryGetValue(context.Script, out var hashset) &&
            hashset.Contains((uint)position))
        {
            BreakAt(context, position);
        }
    }

    private void BreakAt(ExecutionContext context, int position)
    {
        _lastStepPosition = position;
        Engine.State = VMState.BREAK;

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogBreakMessage(LogLevel.Debug, $"Breakpoint hit | IP: {position:X04}");

        OnBreakpoint?.Invoke(this, new DebuggerEventArgs(context));
    }
}

/// <summary>
/// Event arguments for debugger breakpoint notifications.
/// </summary>
/// <param name="context">The execution context at the break site.</param>
public class DebuggerEventArgs(ExecutionContext context) : EventArgs
{
    /// <summary>
    /// Gets the execution context where the breakpoint was hit.
    /// </summary>
    public ExecutionContext Context { get; } = context;
}
