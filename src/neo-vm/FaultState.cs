using System;

namespace Neo.VM
{
    public sealed class FaultState
    {
        private Exception exception;

        public bool Rethrow;
        public bool IsCatchableInterrupt;
        public Exception Exception
        {
            get
            {
                return exception;
            }
            set
            {
                exception = value;
                IsCatchableInterrupt = exception != null && exception is CatcheableException;
            }
        }
    }
}
