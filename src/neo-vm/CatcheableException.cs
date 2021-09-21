using System;

namespace Neo.VM
{
    internal class CatcheableException : Exception
    {
        public CatcheableException(string? message) : base(message)
        {
        }
    }
}
