using Neo.VM;

namespace Neo.Test.Types
{
    public class InteropService : IInteropService
    {
        public bool Invoke(uint method, ExecutionEngine engine)
        {
            if (method == 0x77777777)
            {
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new object()));
                return true;
            }

            return false;
        }
    }
}