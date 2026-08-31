// Copyright (C) 2015-2026 The Neo Project.
//
// VirtualMachineEventId.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Logging;

/// <summary>
/// Well-known event IDs used by VM logger messages.
/// </summary>
public static class VirtualMachineEventId
{
    /// <summary>Event ID for unrecoverable VM faults.</summary>
    public const int Fault = 100;

    /// <summary>Event ID for engine or context creation.</summary>
    public const int Create = 200;

    /// <summary>Event ID for script or context load operations.</summary>
    public const int Load = 201;

    /// <summary>Event ID for paired pre/post lifecycle messages.</summary>
    public const int PrePost = 202;

    /// <summary>Event ID for post-lifecycle messages.</summary>
    public const int Post = 203;

    /// <summary>Event ID for debugger breakpoint stops.</summary>
    public const int Break = 204;

    /// <summary>Event ID for general execution progress messages.</summary>
    public const int Execute = 205;

    /// <summary>Event ID for gas burn operations.</summary>
    public const int Burn = 300;

    /// <summary>Event ID for interop or contract calls.</summary>
    public const int Call = 301;

    /// <summary>Event ID for runtime notify events.</summary>
    public const int Notify = 302;

    /// <summary>Event ID for runtime log events.</summary>
    public const int Log = 303;

    /// <summary>Event ID for block persist start.</summary>
    public const int Persist = 400;

    /// <summary>Event ID for block persist completion.</summary>
    public const int PostPersist = 401;

    /// <summary>Event ID for storage put operations.</summary>
    public const int StoragePut = 500;

    /// <summary>Event ID for storage get operations.</summary>
    public const int StorageGet = 501;

    /// <summary>Event ID for storage find/query operations.</summary>
    public const int StorageFind = 502;

    /// <summary>Event ID for storage delete operations.</summary>
    public const int StorageDelete = 503;

    /// <summary>Event ID for iterator advance operations.</summary>
    public const int IteratorNext = 600;

    /// <summary>Event ID for iterator value retrieval.</summary>
    public const int IteratorGet = 601;

    /// <summary>Event ID for storage read-path diagnostics.</summary>
    public const int ReadStorage = 700;

    /// <summary>Event ID for storage update-path diagnostics.</summary>
    public const int UpdateStorage = 701;
}
