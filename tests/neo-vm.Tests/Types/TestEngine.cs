using Neo.VM;

namespace Neo.Test.Types
{
    class TestEngine : ExecutionEngine
    {
        protected override bool OnSysCall(uint method)
        {
            if (method == 0x77777777)
            {
                CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new object()));
                return true;
            }

            return false;
        }
    }
}
