using System;

namespace Neo.VM
{
    public sealed class FaultState
    {
        private CatcheableException exception;

        public bool Rethrow;
        public bool HasCatchableInterrupt;
        public CatcheableException Exception
        {
            get
            {
                return exception;
            }
            set
            {
                exception = value;
                HasCatchableInterrupt = exception != null && exception is CatcheableException;
            }
        }
    }
}
