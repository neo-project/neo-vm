using System;
using System.Collections.Generic;

namespace AntShares.VM
{
    public class ApiService
    {
        private Dictionary<string, Func<ScriptEngine, bool>> dictionary = new Dictionary<string, Func<ScriptEngine, bool>>();

        public ApiService()
        {
            Register("System.ScriptEngine.GetScriptContainer", GetScriptContainer);
            Register("System.ScriptEngine.GetExecutingScriptHash", GetExecutingScriptHash);
            Register("System.ScriptEngine.GetCallingScriptHash", GetCallingScriptHash);
            Register("System.ScriptEngine.GetEntryScriptHash", GetEntryScriptHash);
        }

        protected bool Register(string method, Func<ScriptEngine, bool> handler)
        {
            if (dictionary.ContainsKey(method)) return false;
            dictionary.Add(method, handler);
            return true;
        }

        internal bool Invoke(string method, ScriptEngine engine)
        {
            if (!dictionary.ContainsKey(method)) return false;
            return dictionary[method](engine);
        }

        private static bool GetScriptContainer(ScriptEngine engine)
        {
            engine.EvaluationStack.Push(new StackItem(engine.ScriptContainer));
            return true;
        }

        private static bool GetExecutingScriptHash(ScriptEngine engine)
        {
            engine.EvaluationStack.Push(engine.Crypto.Hash160(engine.ExecutingScript));
            return true;
        }

        private static bool GetCallingScriptHash(ScriptEngine engine)
        {
            engine.EvaluationStack.Push(engine.Crypto.Hash160(engine.CallingScript));
            return true;
        }

        private static bool GetEntryScriptHash(ScriptEngine engine)
        {
            engine.EvaluationStack.Push(engine.Crypto.Hash160(engine.EntryScript));
            return true;
        }
    }
}
