using Neo.Test.Cryptography;
using System;
using System.Security.Cryptography;
using System.Threading;

namespace Neo.Test.Extensions
{
    public static class ByteArrayExtensions
    {
        private static ThreadLocal<SHA256> _sha256 = new ThreadLocal<SHA256>(() => SHA256.Create());
        private static ThreadLocal<RIPEMD160Managed> _ripemd160 = new ThreadLocal<RIPEMD160Managed>(() => new RIPEMD160Managed());

        public static ReadOnlySpan<byte> RIPEMD160(this ReadOnlySpan<byte> value)
        {
            return _ripemd160.Value.ComputeHash(value.ToArray());
        }

        public static ReadOnlySpan<byte> Sha256(this ReadOnlySpan<byte> value)
        {
            return _sha256.Value.ComputeHash(value.ToArray());
        }
    }
}
