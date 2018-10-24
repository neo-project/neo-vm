using System;
using System.Collections.Generic;

namespace Neo.VM
{
    public class InteropService
    {
        private Dictionary<uint, Func<ExecutionEngine, bool>> dictionary = new Dictionary<uint, Func<ExecutionEngine, bool>>();
        private Dictionary<string, uint> dictionaryStr = new Dictionary<string, uint>();

        public uint InteropHash(string method)
        {
            return dictionaryStr[method];
        }

        public InteropService()
        {
            Register("System.ExecutionEngine.GetScriptContainer", GetScriptContainer);
            Register("System.ExecutionEngine.GetExecutingScriptHash", GetExecutingScriptHash);
            Register("System.ExecutionEngine.GetCallingScriptHash", GetCallingScriptHash);
            Register("System.ExecutionEngine.GetEntryScriptHash", GetEntryScriptHash);
        }

        protected void Register(string method, Func<ExecutionEngine, bool> handler)
        {
            uint hash = method.ToInteropMethodHash();
            dictionary[hash] = handler;
            dictionaryStr[method] = hash;
        }

        internal bool Invoke(byte[] method, ExecutionEngine engine)
        {
            uint hash = method.Length == 4
                ? BitConverter.ToUInt32(method, 0)
                : dictionaryStr[method];
            if (!dictionary.TryGetValue(hash, out Func<ExecutionEngine, bool> func)) return false;
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
