// Copyright (C) 2015-2026 The Neo Project.
//
// Debugger.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.VM.Middleware;

namespace Neo.VM;

/// <summary>
/// A debugger for <see cref="ExecutionEngine"/> implemented as engine middleware.
/// </summary>
public class Debugger : DebuggerMiddleware
{
    /// <summary>
    /// Create a debugger on the specified <see cref="ExecutionEngine"/>.
    /// The debugger is registered on the engine pipeline.
    /// </summary>
    /// <param name="engine">The <see cref="ExecutionEngine"/> to attach the debugger.</param>
    public Debugger(ExecutionEngine engine)
        : base(engine)
    {
    }

    /// <summary>
    /// Create a debugger on the specified <see cref="ExecutionEngine"/> with a logger.
    /// The debugger is registered on the engine pipeline.
    /// </summary>
    /// <param name="engine">The <see cref="ExecutionEngine"/> to attach the debugger.</param>
    /// <param name="logger">Logger that receives debugger messages.</param>
    public Debugger(ExecutionEngine engine, ILogger logger)
        : base(engine, logger)
    {
    }
}
