using System;

namespace Neo.VM
{
    public interface IInteropService
    {
        void Register(string method, Func<ExecutionEngine, bool> handler);

        bool Invoke(byte[] method, ExecutionEngine engine);
    }
}