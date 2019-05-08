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
                case "System.ExecutionEngine.GetScriptContainer":
                    {
                        engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(engine.ScriptContainer));
                        return true;
                    }
            }

            return false;
        }
    }
}