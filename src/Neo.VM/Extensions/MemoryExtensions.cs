// Copyright (C) 2015-2026 The Neo Project.
//
// MemoryExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.VM.Extensions;

/// <summary>
/// Hash helpers for <see cref="Memory{T}"/>.
/// </summary>
public static class MemoryExtensions
{
    /// <summary>
    /// Computes a stable integer hash over the memory contents.
    /// </summary>
    public static int ToHashCode(this Memory<byte> data, int seed)
        => data.Span.ToHashCode(seed);

    /// <summary>
    /// Computes a stable integer hash seeded by the memory length.
    /// </summary>
    public static int ToHashCode(this Memory<byte> data)
        => data.Span.ToHashCode(data.Length);

    /// <summary>
    /// Computes a stable integer hash seeded by the memory length.
    /// </summary>
    public static int ToHashCode(this ReadOnlyMemory<byte> data)
        => data.Span.ToHashCode(data.Length);

    /// <summary>
    /// Computes a stable integer hash over the memory contents.
    /// </summary>
    public static int ToHashCode(this ReadOnlyMemory<byte> data, int seed)
        => data.Span.ToHashCode(seed);
}
