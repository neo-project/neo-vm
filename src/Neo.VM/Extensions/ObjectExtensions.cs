// Copyright (C) 2015-2026 The Neo Project.
//
// ObjectExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System;
using System.Collections;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.VM.Extensions;

/// <summary>
/// Converts CLR values to <see cref="StackItem"/>.
/// </summary>
public static class ObjectExtensions
{
    /// <summary>
    /// Converts a CLR value into the corresponding <see cref="StackItem"/>.
    /// </summary>
    public static StackItem ToStackItem(this object? source)
        => source switch
        {
            null => StackItem.Null,
            StackItem item => item,
            bool b => b,
            byte[] ba => ba,
            byte b => b,
            sbyte b => b,
            short s => s,
            ushort s => s,
            int i => i,
            uint i => i,
            long l => l,
            ulong l => l,
            BigInteger bi => bi,
            string s => s,
            Memory<byte> m => new ByteString(m),
            ReadOnlyMemory<byte> rm => (ByteString)rm,
            IDictionary d => DictionaryToMap(d),
            IEnumerable e => new Array(e.Cast<object>().Select(static s => s.ToStackItem())),
            _ => new InteropInterface(source)
        };

    private static Map DictionaryToMap(IDictionary source)
    {
        var map = new Map();
        foreach (DictionaryEntry entry in source)
        {
            var key = entry.Key.ToStackItem();
            if (key is not PrimitiveType primitive)
                throw new InvalidCastException($"Dictionary key {key.Type} is not a {nameof(PrimitiveType)}.");
            map[primitive] = entry.Value.ToStackItem();
        }
        return map;
    }
}
