namespace Neo.VM
{
    public interface IExecutionContextFactory
    {
        ExecutionContext CreateExecutionContext(Script script, Script callingScript, int rvcount);
    }
}