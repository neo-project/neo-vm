using Neo.VM.Types;
using System;

namespace Neo.VM
{
    public class CatcheableException : Exception
    {
        public StackItem Error;

        public CatcheableException(StackItem error)
        {
            Error = error;
        }
    }
}
