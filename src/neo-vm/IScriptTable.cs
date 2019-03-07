namespace Neo.VM
{
    //一个Script 可能是一个 byte[] NEOVM,也可能是一个NativeContract
    public interface IScriptTable
    {
        IScript GetScript(byte[] script_hash);
    }
}
