// Copyright (C) 2015-2026 The Neo Project.
//
// EnumerableExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;

namespace Neo.VM.Extensions;

/// <summary>
/// Hash helpers for sequences.
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Computes a stable integer hash over the sequence elements.
    /// </summary>
    public static int ToHashCode<TSource>(this IEnumerable<TSource> source, int seed = 397)
    {
        var hash = seed;
        foreach (var item in source)
            hash = unchecked((hash * 31) ^ (item?.GetHashCode() ?? 0));
        return hash;
    }
}
