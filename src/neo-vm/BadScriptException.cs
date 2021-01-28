using System;

namespace Neo.VM
{
    public class BadScriptException : Exception
    {
        public BadScriptException() { }
        public BadScriptException(string message) : base(message) { }
    }
}
