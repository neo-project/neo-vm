namespace Neo.VM
{
    public interface IScriptTable
    {
        IScript GetScript(byte[] script_hash);
    }
}
