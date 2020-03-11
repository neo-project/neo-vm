using System;

namespace Neo.VM
{
    public class FaultState
    {
        public bool Rethrow;
        public Exception Exception;
        public bool IsCatcheableException => Exception != null && Exception is CatcheableException;
    }
}
