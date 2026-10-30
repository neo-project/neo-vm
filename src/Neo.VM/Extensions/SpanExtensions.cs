// Copyright (C) 2015-2026 The Neo Project.
//
// SpanExtensions.cs file belongs to the neo project and is free
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
/// Hash helpers for <see cref="ReadOnlySpan{T}"/>.
/// </summary>
public static class SpanExtensions
{
    /// <summary>
    /// Computes a stable integer hash over the byte span.
    /// </summary>
    public static int ToHashCode(this ReadOnlySpan<byte> data, int seed = 397)
    {
        var hash = seed;
        foreach (var b in data)
            hash = unchecked((hash * 31) ^ b);
        return hash;
    }
}
