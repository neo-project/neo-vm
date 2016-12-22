namespace AntShares.VM
{
    public class ApiService
    {
        public virtual bool Invoke(string method, ScriptEngine engine)
        {
            switch (method)
            {
                case "System.ScriptEngine.GetScriptContainer":
                    return GetScriptContainer(engine);
                case "System.ScriptEngine.GetExecutingScriptHash":
                    return GetExecutingScriptHash(engine);
                case "System.ScriptEngine.GetCallingScriptHash":
                    return GetCallingScriptHash(engine);
                case "System.ScriptEngine.GetEntryScriptHash":
                    return GetEntryScriptHash(engine);
                default:
                    return false;
            }
        }

        private bool GetScriptContainer(ScriptEngine engine)
        {
            engine.EvaluationStack.Push(new StackItem(engine.ScriptContainer));
            return true;
        }

        private bool GetExecutingScriptHash(ScriptEngine engine)
        {
            engine.EvaluationStack.Push(engine.Crypto.Hash160(engine.ExecutingScript));
            return true;
        }

        private bool GetCallingScriptHash(ScriptEngine engine)
        {
            engine.EvaluationStack.Push(engine.Crypto.Hash160(engine.CallingScript));
            return true;
        }

        private bool GetEntryScriptHash(ScriptEngine engine)
        {
            engine.EvaluationStack.Push(engine.Crypto.Hash160(engine.EntryScript));
            return true;
        }
    }
}
