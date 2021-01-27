using System;

namespace Neo.VM
{
    public class WrongScriptException : Exception
    {
        public WrongScriptException(string message) : base(message)
        {
        }
    }
}
