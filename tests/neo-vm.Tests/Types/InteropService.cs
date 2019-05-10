using System.Text;
using Neo.VM;

namespace Neo.Test.Types
{
    public class InteropService : IInteropService
    {
        public bool Invoke(byte[] method, ExecutionEngine engine)
        {
            switch (Encoding.ASCII.GetString(method))
            {
                case "Test.ExecutionEngine.GetScriptContainer":
                    {
                        engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new object()));
                        return true;
                    }
            }

            return false;
        }
    }
}