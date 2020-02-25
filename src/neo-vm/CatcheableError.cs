using System;

namespace Neo.VM
{
    public class CatcheableError : Exception
    {
        public CatcheableError(string message) : base(message)
        {
        }
    }
}
