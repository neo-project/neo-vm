namespace AntShares.VM
{
    public interface IScriptContainer : IInteropInterface
    {
        byte[] GetMessage();
    }
}
