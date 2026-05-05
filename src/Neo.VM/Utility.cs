// Copyright (C) 2015-2026 The Neo Project.
//
// Utility.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Hashing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Neo.VM;

static class Utility
{
    public static Encoding StrictUTF8 { get; }

    private const int DefaultXxHash3Seed = 40343;

    static Utility()
    {
        StrictUTF8 = (Encoding)Encoding.UTF8.Clone();
        StrictUTF8.DecoderFallback = DecoderFallback.ExceptionFallback;
        StrictUTF8.EncoderFallback = EncoderFallback.ExceptionFallback;
    }

    /// <summary>
    /// Converts a byte span to a strict UTF8 string.
    /// </summary>
    /// <param name="bytes">The byte span to convert.</param>
    /// <param name="value">The converted string.</param>
    /// <returns>True if the conversion is successful, otherwise false.</returns>
    public static bool TryToStrictUtf8String(this ReadOnlySpan<byte> bytes, [NotNullWhen(true)] out string? value)
    {
        try
        {
            value = StrictUTF8.GetString(bytes);
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    /// <summary>
    /// Converts a byte span to a strict UTF8 string.
    /// </summary>
    /// <param name="value">The byte span to convert.</param>
    /// <returns>The converted string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToStrictUtf8String(this ReadOnlySpan<byte> value) => StrictUTF8.GetString(value);

    /// <summary>
    /// Converts a string to a strict UTF8 byte array.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The converted byte array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] ToStrictUtf8Bytes(this string value) => StrictUTF8.GetBytes(value);

    /// <summary>
    /// Computes the 32-bit hash value for the specified byte array using the xxhash3 algorithm.
    /// </summary>
    /// <param name="value">The input to compute the hash code for.</param>
    /// <param name="seed">The seed used by the xxhash3 algorithm.</param>
    /// <returns>The computed hash code.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int XxHash3_32(this ReadOnlySpan<byte> value, long seed = DefaultXxHash3Seed)
    {
        return HashCode.Combine(XxHash3.HashToUInt64(value, seed));
    }

    public static BigInteger ModInverse(this BigInteger value, BigInteger modulus)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        ArgumentOutOfRangeException.ThrowIfLessThan(modulus, 2);
        BigInteger r = value, old_r = modulus, s = 1, old_s = 0;
        while (r > 0)
        {
            BigInteger q = old_r / r;
            (old_r, r) = (r, old_r % r);
            (old_s, s) = (s, old_s - q * s);
        }
        BigInteger result = old_s % modulus;
        if (result < 0) result += modulus;
        if (!(value * result % modulus).IsOne) throw new InvalidOperationException();
        return result;
    }

    public static BigInteger Sqrt(this BigInteger value)
    {
        if (value < 0) throw new InvalidOperationException("value can not be negative");
        if (value.IsZero) return BigInteger.Zero;
        if (value < 4) return BigInteger.One;

        var z = value;
        var x = BigInteger.One << (int)(((value - 1).GetBitLength() + 1) >> 1);
        while (x < z)
        {
            z = x;
            x = (value / x + x) / 2;
        }

        return z;
    }
}
