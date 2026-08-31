// Copyright (C) 2015-2026 The Neo Project.
//
// LoggerExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.VM.Logging;
using System;

namespace Neo.VM.Extensions;

/// <summary>
/// Source-generated logging helpers for virtual machine execution events.
/// One method per <see cref="VirtualMachineEventId"/>.
/// </summary>
public static partial class LoggerExtensions
{
    /// <summary>Logs a VM fault message.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.Fault, EventName = nameof(VirtualMachineEventId.Fault), Message = "{Message}")]
    public static partial void LogFaultMessage(this ILogger logger, LogLevel logLevel, string message);

    /// <summary>Logs a VM fault with an exception.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.Fault, EventName = "FaultException", Message = "{Message}")]
    public static partial void LogFaultMessage(this ILogger logger, LogLevel logLevel, Exception exception, string message);

    /// <summary>Logs a VM engine or context creation message.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.Create, EventName = nameof(VirtualMachineEventId.Create), Message = "{Message}")]
    public static partial void LogCreateMessage(this ILogger logger, LogLevel logLevel, string message);

    /// <summary>Logs a VM load message.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.Load, EventName = nameof(VirtualMachineEventId.Load), Message = "{Message}")]
    public static partial void LogLoadMessage(this ILogger logger, LogLevel logLevel, string message);

    /// <summary>Logs a paired pre/post lifecycle message.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.PrePost, EventName = nameof(VirtualMachineEventId.PrePost), Message = "{Message}")]
    public static partial void LogPrePostMessage(this ILogger logger, LogLevel logLevel, string message);

    /// <summary>Logs a post-lifecycle message.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.Post, EventName = nameof(VirtualMachineEventId.Post), Message = "{Message}")]
    public static partial void LogPostMessage(this ILogger logger, LogLevel logLevel, string message);

    /// <summary>Logs a VM break (debugger breakpoint) message.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.Break, EventName = nameof(VirtualMachineEventId.Break), Message = "{Message}")]
    public static partial void LogBreakMessage(this ILogger logger, LogLevel logLevel, string message);

    /// <summary>Logs a VM execute message.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.Execute, EventName = nameof(VirtualMachineEventId.Execute), Message = "{Message}")]
    public static partial void LogExecuteMessage(this ILogger logger, LogLevel logLevel, string message);

    /// <summary>Logs a gas burn message.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.Burn, EventName = nameof(VirtualMachineEventId.Burn), Message = "{Message}")]
    public static partial void LogBurnMessage(this ILogger logger, LogLevel logLevel, string message);

    /// <summary>Logs an interop or contract call message.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.Call, EventName = nameof(VirtualMachineEventId.Call), Message = "{Message}")]
    public static partial void LogCallMessage(this ILogger logger, LogLevel logLevel, string message);

    /// <summary>Logs a runtime notify message.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.Notify, EventName = nameof(VirtualMachineEventId.Notify), Message = "{Message}")]
    public static partial void LogNotifyMessage(this ILogger logger, LogLevel logLevel, string message);

    /// <summary>Logs a runtime log message.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.Log, EventName = nameof(VirtualMachineEventId.Log), Message = "{Message}")]
    public static partial void LogLogMessage(this ILogger logger, LogLevel logLevel, string message);

    /// <summary>Logs a block persist start message.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.Persist, EventName = nameof(VirtualMachineEventId.Persist), Message = "{Message}")]
    public static partial void LogPersistMessage(this ILogger logger, LogLevel logLevel, string message);

    /// <summary>Logs a block persist completion message.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.PostPersist, EventName = nameof(VirtualMachineEventId.PostPersist), Message = "{Message}")]
    public static partial void LogPostPersistMessage(this ILogger logger, LogLevel logLevel, string message);

    /// <summary>Logs a storage put message.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.StoragePut, EventName = nameof(VirtualMachineEventId.StoragePut), Message = "{Message}")]
    public static partial void LogStoragePutMessage(this ILogger logger, LogLevel logLevel, string message);

    /// <summary>Logs a storage get message.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.StorageGet, EventName = nameof(VirtualMachineEventId.StorageGet), Message = "{Message}")]
    public static partial void LogStorageGetMessage(this ILogger logger, LogLevel logLevel, string message);

    /// <summary>Logs a storage find/query message.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.StorageFind, EventName = nameof(VirtualMachineEventId.StorageFind), Message = "{Message}")]
    public static partial void LogStorageFindMessage(this ILogger logger, LogLevel logLevel, string message);

    /// <summary>Logs a storage delete message.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.StorageDelete, EventName = nameof(VirtualMachineEventId.StorageDelete), Message = "{Message}")]
    public static partial void LogStorageDeleteMessage(this ILogger logger, LogLevel logLevel, string message);

    /// <summary>Logs an iterator advance message.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.IteratorNext, EventName = nameof(VirtualMachineEventId.IteratorNext), Message = "{Message}")]
    public static partial void LogIteratorNextMessage(this ILogger logger, LogLevel logLevel, string message);

    /// <summary>Logs an iterator value retrieval message.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.IteratorGet, EventName = nameof(VirtualMachineEventId.IteratorGet), Message = "{Message}")]
    public static partial void LogIteratorGetMessage(this ILogger logger, LogLevel logLevel, string message);

    /// <summary>Logs a storage read-path diagnostic message.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.ReadStorage, EventName = nameof(VirtualMachineEventId.ReadStorage), Message = "{Message}")]
    public static partial void LogReadStorageMessage(this ILogger logger, LogLevel logLevel, string message);

    /// <summary>Logs a storage update-path diagnostic message.</summary>
    [LoggerMessage(EventId = VirtualMachineEventId.UpdateStorage, EventName = nameof(VirtualMachineEventId.UpdateStorage), Message = "{Message}")]
    public static partial void LogUpdateStorageMessage(this ILogger logger, LogLevel logLevel, string message);
}
