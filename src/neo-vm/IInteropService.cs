namespace Neo.VM
{
    public interface IInteropService
    {
        bool Invoke(uint method, ExecutionEngine engine);
    }
}
