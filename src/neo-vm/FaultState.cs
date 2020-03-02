using System;

namespace Neo.VM
{
    public class FaultState
    {
        public bool Rethrow;
        public Exception Error;
        public bool IsCatcheableError => Error != null && Error is CatcheableError;
    }
}
