// Copyright (C) 2016-2021 The Neo Project.
// 
// The neo-vm is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Numerics;
using System.Text;

namespace Neo.VM
{
    internal static class Utility
    {
        public static Encoding StrictUTF8 { get; }

        static Utility()
        {
            StrictUTF8 = (Encoding)Encoding.UTF8.Clone();
            StrictUTF8.DecoderFallback = DecoderFallback.ExceptionFallback;
            StrictUTF8.EncoderFallback = EncoderFallback.ExceptionFallback;
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
}
