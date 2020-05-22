using System.Diagnostics;

namespace Neo.VM
{
    [DebuggerDisplay("State={State}, CatchPointer={CatchPointer}, FinallyPointer={FinallyPointer}, EndPointer={EndPointer}")]
    public sealed class ExceptionHandlingContext
    {
        public int CatchPointer { get; }
        public int FinallyPointer { get; }
        public int EndPointer { get; internal set; } = -1;
        public bool HasCatch => CatchPointer >= 0;
        public bool HasFinally => FinallyPointer >= 0;
        public ExceptionHandlingState State { get; internal set; } = ExceptionHandlingState.Try;

        internal ExceptionHandlingContext(int catchPointer, int finallyPointer)
        {
            this.CatchPointer = catchPointer;
            this.FinallyPointer = finallyPointer;
        }
    }
}
