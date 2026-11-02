// Copyright (C) 2015-2026 The Neo Project.
//
// ListExtensions.cs file belongs to the neo project and is free
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
/// Hash helpers for lists.
/// </summary>
public static class ListExtensions
{
    /// <summary>
    /// Computes a stable integer hash over the list elements.
    /// </summary>
    public static int ToHashCode<TSource>(this IList<TSource> source, int seed = 397)
        => EnumerableExtensions.ToHashCode(source, seed);

    /// <summary>
    /// Computes a stable integer hash over the read-only list elements.
    /// </summary>
    public static int ToHashCode<TSource>(this IReadOnlyList<TSource> source, int seed = 397)
        => EnumerableExtensions.ToHashCode(source, seed);
}
