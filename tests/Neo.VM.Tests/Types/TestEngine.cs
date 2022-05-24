using Neo.VM;
using Neo.VM.Types;

namespace Neo.Test.Types
{
    class TestEngine : ExecutionEngine
    {
        protected override void OnSysCall(uint method)
        {
            if (method == 0x77777777)
            {
                CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new object()));
                return;
            }

            if (method == 0xaddeadde)
            {
                ExecuteThrow("error");
                return;
            }

            throw new System.Exception();
        }
    }
}
