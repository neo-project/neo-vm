using System;
using System.Collections.Generic;

namespace Neo.VM
{
    public class InteropService
    {
        private Dictionary<uint, Func<ExecutionEngine, bool>> dictionary = new Dictionary<uint, Func<ExecutionEngine, bool>>();

        public InteropService()
        {
            Register("System.ExecutionEngine.GetScriptContainer", GetScriptContainer);
            Register("System.ExecutionEngine.GetExecutingScriptHash", GetExecutingScriptHash);
            Register("System.ExecutionEngine.GetCallingScriptHash", GetCallingScriptHash);
            Register("System.ExecutionEngine.GetEntryScriptHash", GetEntryScriptHash);
        }

        protected void Register(string method, Func<ExecutionEngine, bool> handler)
        {
            uint key = Crypto.Default.Hash256(Encoding.ASCII.GetBytes(method)).ToUInt32(0);
            dictionary[key] = handler;
        }

        internal bool Invoke(string method, ExecutionEngine engine)
        {
            uint key = Crypto.Default.Hash256(Encoding.ASCII.GetBytes(method)).ToUInt32(0);
            return Invoke(key, engine);
        }

        internal bool Invoke(uint key, ExecutionEngine engine)
        {
            if (!dictionary.TryGetValue(key, out Func<ExecutionEngine, bool> func)) return false;
            return func(engine);
        }

        private static bool GetScriptContainer(ExecutionEngine engine)
        {
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(engine.ScriptContainer));
            return true;
        }

        private static bool GetExecutingScriptHash(ExecutionEngine engine)
        {
            engine.CurrentContext.EvaluationStack.Push(engine.CurrentContext.ScriptHash);
            return true;
        }

        private static bool GetCallingScriptHash(ExecutionEngine engine)
        {
            engine.CurrentContext.EvaluationStack.Push(engine.CallingContext.ScriptHash);
            return true;
        }

        private static bool GetEntryScriptHash(ExecutionEngine engine)
        {
            engine.CurrentContext.EvaluationStack.Push(engine.EntryContext.ScriptHash);
            return true;
        }
    }
}
