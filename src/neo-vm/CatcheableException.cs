using Neo.VM.Types;
using System;

namespace Neo.VM
{
    public class CatcheableException : Exception
    {
        public StackItem ExceptionItem;

        public CatcheableException(StackItem item)
        {
            ExceptionItem = item;
        }
    }
}
