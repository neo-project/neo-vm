using System.Diagnostics;

namespace Neo.VM
{
    [DebuggerDisplay("State={State}, CatchPointer={CatchPointer}, FinallyPointer={FinallyPointer}, EndPointer={EndPointer}")]
    public sealed class ExceptionHandingContext
    {
        public ExecutionContext ExecutionContext { get; private set; }
        public int CatchPointer { get; private set; }
        public int FinallyPointer { get; private set; }
        public int EndPointer { get; internal set; }
        public bool HasCatch { get; private set; }
        public bool HasFinally { get; private set; }
        public ExceptionHandingState State { get; internal set; } = ExceptionHandingState.Try;

        public ExceptionHandingContext(ExecutionContext ExecutionContext, int catchOffset, int finallyOffset)
        {
            this.ExecutionContext = ExecutionContext;
            this.HasCatch = catchOffset != 0;
            this.HasFinally = finallyOffset != 0;
            this.CatchPointer = checked(ExecutionContext.InstructionPointer + catchOffset);
            this.FinallyPointer = checked(ExecutionContext.InstructionPointer + finallyOffset);
        }
    }
}
