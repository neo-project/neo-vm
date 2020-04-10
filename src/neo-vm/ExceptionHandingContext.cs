using System.Diagnostics;

namespace Neo.VM
{
    [DebuggerDisplay("State={State}, CatchPointer={CatchPointer}, FinallyPointer={FinallyPointer}, EndPointer={EndPointer}")]
    public sealed class ExceptionHandingContext
    {
        public ExecutionContext ExecutionContext { get; private set; }
        public int CatchPointer { get; } = -1;
        public int FinallyPointer { get; } = -1;
        public int EndPointer { get; internal set; } = -1;
        public bool HasCatch => CatchPointer >= 0;
        public bool HasFinally => FinallyPointer >= 0;
        public ExceptionHandingState State { get; internal set; } = ExceptionHandingState.Try;

        public ExceptionHandingContext(ExecutionContext ExecutionContext, int catchOffset, int finallyOffset)
        {
            this.ExecutionContext = ExecutionContext;
            if (catchOffset != 0)
                this.CatchPointer = checked(ExecutionContext.InstructionPointer + catchOffset);
            if (finallyOffset != 0)
                this.FinallyPointer = checked(ExecutionContext.InstructionPointer + finallyOffset);
        }
    }
}
