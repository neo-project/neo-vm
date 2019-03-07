namespace Neo.VM
{
    public interface IScriptTable
    {
        byte[] GetScript(byte[] script_hash);

        void IncrementInvocationCounter(byte[] script_hash);
        
        int GetInvocationCounter(byte[] script_hash);
    }
}
