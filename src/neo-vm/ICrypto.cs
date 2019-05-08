namespace Neo.VM
{
    public interface ICrypto
    {
        bool VerifySignature(byte[] message, byte[] signature, byte[] pubkey);
    }
}
