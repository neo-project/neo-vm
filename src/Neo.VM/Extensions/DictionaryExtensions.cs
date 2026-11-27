// Copyright (C) 2015-2026 The Neo Project.
//
// DictionaryExtensions.cs file belongs to the neo project and is free
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
/// Hash helpers for dictionaries.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Computes a stable integer hash over the dictionary entries.
    /// </summary>
    public static int ToHashCode<TKey, TValue>(this IDictionary<TKey, TValue> source, int seed = 397)
    {
        var hash = seed;
        foreach (var (key, value) in source)
            hash = unchecked((hash * 31) + ((key?.GetHashCode() ?? 0) ^ (value?.GetHashCode() ?? 0)));
        return hash;
    }

    /// <summary>
    /// Computes a stable integer hash over the read-only dictionary entries.
    /// </summary>
    public static int ToHashCode<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, int seed = 397)
    {
        var hash = seed;
        foreach (var (key, value) in source)
            hash = unchecked((hash * 31) + ((key?.GetHashCode() ?? 0) ^ (value?.GetHashCode() ?? 0)));
        return hash;
    }
}
