using System;

namespace Neo.VM
{
    public class CatchableException : Exception
    {
        public CatchableException(string message) : base(message)
        {
        }
    }
}
