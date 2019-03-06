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
                case "System.ExecutionEngine.GetEntryScriptHash":
                    {
                        engine.CurrentContext.EvaluationStack.Push(engine.EntryContext.ScriptHash);
                        return true;
                    }
                case "System.ExecutionEngine.GetCallingScriptHash":
                    {
                        engine.CurrentContext.EvaluationStack.Push(engine.CallingContext.ScriptHash);
                        return true;
                    }
                case "System.ExecutionEngine.GetExecutingScriptHash":
                    {
                        engine.CurrentContext.EvaluationStack.Push(engine.CurrentContext.ScriptHash);
                        return true;
                    }
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