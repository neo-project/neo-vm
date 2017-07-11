using System;
using System.Collections.Generic;

namespace Neo.VM
{
    public class InteropService
    {
        private Dictionary<string, Func<ExecutionEngine, bool>> dictionary = new Dictionary<string, Func<ExecutionEngine, bool>>();

        public InteropService()
        {
            Register("System.ExecutionEngine.GetScriptContainer", GetScriptContainer);
            Register("System.ExecutionEngine.GetExecutingScriptHash", GetExecutingScriptHash);
            Register("System.ExecutionEngine.GetCallingScriptHash", GetCallingScriptHash);
            Register("System.ExecutionEngine.GetEntryScriptHash", GetEntryScriptHash);
        }

        protected void Register(string method, Func<ExecutionEngine, bool> handler)
        {
            dictionary[method] = handler;
        }

        internal bool Invoke(string method, ExecutionEngine engine)
        {
            if (!dictionary.ContainsKey(method)) return false;
            return dictionary[method](engine);
        }

        private static bool GetScriptContainer(ExecutionEngine engine)
        {
            engine.EvaluationStack.Push(StackItem.FromInterface(engine.ScriptContainer));
            return true;
        }

        private static bool GetExecutingScriptHash(ExecutionEngine engine)
        {
            engine.EvaluationStack.Push(engine.CurrentContext.ScriptHash);
            return true;
        }

        private static bool GetCallingScriptHash(ExecutionEngine engine)
        {
            engine.EvaluationStack.Push(engine.CallingContext.ScriptHash);
            return true;
        }

        private static bool GetEntryScriptHash(ExecutionEngine engine)
        {
            engine.EvaluationStack.Push(engine.EntryContext.ScriptHash);
            return true;
        }
    }
}
