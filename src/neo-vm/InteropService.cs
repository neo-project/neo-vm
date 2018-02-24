﻿using System;
using System.Collections.Generic;

namespace Neo.VM
{
    public class InteropService
    {
        public delegate bool delInterop(ExecutionEngine engine);

        private Dictionary<string, delInterop> dictionary = new Dictionary<string, delInterop>();

        public InteropService()
        {
            Register("System.ExecutionEngine.GetScriptContainer", GetScriptContainer);
            Register("System.ExecutionEngine.GetExecutingScriptHash", GetExecutingScriptHash);
            Register("System.ExecutionEngine.GetCallingScriptHash", GetCallingScriptHash);
            Register("System.ExecutionEngine.GetEntryScriptHash", GetEntryScriptHash);
        }

        protected void Register(string method, delInterop handler)
        {
            dictionary[method] = handler;
        }

        internal bool Invoke(string method, ExecutionEngine engine)
        {
            if (!dictionary.TryGetValue(method, out delInterop func)) return false;
            return func(engine);
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
