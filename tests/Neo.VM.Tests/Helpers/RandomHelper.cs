// Copyright (C) 2015-2026 The Neo Project.
//
// RandomHelper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Threading;

namespace Neo.Test.Helpers;

public class RandomHelper
{
    private const string _randchars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    private static readonly ThreadLocal<Random> _rand = new(() => new Random());

    /// <summary>
    /// Get random buffer
    /// </summary>
    /// <param name="length">Length</param>
    /// <returns>Buffer</returns>
    public static byte[] RandBuffer(int length)
    {
        var buffer = new byte[length];
        _rand.Value.NextBytes(buffer);
        return buffer;
    }

    /// <summary>
    /// Get random string
    /// </summary>
    /// <param name="length">Length</param>
    /// <returns>Buffer</returns>
    public static string RandString(int length)
    {
        var stringChars = new char[length];

        for (int i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = _randchars[_rand.Value.Next(_randchars.Length)];
        }

        return new string(stringChars);
    }
}
