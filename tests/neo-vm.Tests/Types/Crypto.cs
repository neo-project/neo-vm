using Neo.Test.Extensions;
using Neo.VM;
using System;
using System.Security.Cryptography;

namespace Neo.Test.Types
{
    public class Crypto : ICrypto
    {
        public static readonly Crypto Default = new Crypto();

        public byte[] Hash160(ReadOnlySpan<byte> message)
        {
            return message.Sha256().RIPEMD160().ToArray();
        }

        public byte[] Hash256(ReadOnlySpan<byte> message)
        {
            return message.Sha256().Sha256().ToArray();
        }

        public byte[] Sign(ReadOnlySpan<byte> message, ReadOnlySpan<byte> prikey, ReadOnlySpan<byte> pubkey)
        {
            using (var ecdsa = ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D = prikey.ToArray(),
                Q = new ECPoint
                {
                    X = pubkey.Slice(0, 32).ToArray(),
                    Y = pubkey.Slice(32).ToArray()
                }
            }))
            {
                return ecdsa.SignData(message.ToArray(), HashAlgorithmName.SHA256);
            }
        }

        public bool VerifySignature(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> pubkey)
        {
            if (pubkey.Length == 33 && (pubkey[0] == 0x02 || pubkey[0] == 0x03))
            {
                try
                {
                    pubkey = Neo.Cryptography.ECC.ECPoint.DecodePoint(pubkey.ToArray(), Neo.Cryptography.ECC.ECCurve.Secp256r1).EncodePoint(false);
                    pubkey = pubkey.Slice(1);
                }
                catch
                {
                    return false;
                }
            }
            else if (pubkey.Length == 65 && pubkey[0] == 0x04)
            {
                pubkey = pubkey.Slice(1);
            }
            else if (pubkey.Length != 64)
            {
                throw new ArgumentException();
            }
            using (var ecdsa = ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = pubkey.Slice(0, 32).ToArray(),
                    Y = pubkey.Slice(32).ToArray()
                }
            }))
            {
                return ecdsa.VerifyData(message, signature, HashAlgorithmName.SHA256);
            }
        }
    }
}
