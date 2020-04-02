using System.Collections.Generic;

namespace Neo.VM
{
    public sealed class FaultState
    {
        private CatcheableException exception;

        public bool Rethrow;
        public bool HasCatchableInterrupt;
        public Stack<CatcheableException> CatchedExceptionStack { get; } = new Stack<CatcheableException>();

        public CatcheableException Exception
        {
            get
            {
                return exception;
            }
            set
            {
                exception = value;
                HasCatchableInterrupt = exception != null;
            }
        }
    }
}
