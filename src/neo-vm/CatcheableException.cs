using System;

namespace Neo.VM
{
    public class CatcheableException : Exception
    {
        public CatcheableException(string message) : base(message)
        {
        }
    }
}
