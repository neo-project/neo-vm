using System;

namespace Neo.VM
{
    public interface ICrypto
    {
        byte[] Hash160(ReadOnlySpan<byte> message);

        byte[] Hash256(ReadOnlySpan<byte> message);

        bool VerifySignature(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> pubkey);
    }
}
