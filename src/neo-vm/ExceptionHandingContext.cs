using System.Diagnostics;

namespace Neo.VM
{
    [DebuggerDisplay("State={State}, TryPointer={TryPointer}, CatchPointer={CatchPointer}, FinallyPointer={FinallyPointer}, EndPointer={EndPointer}")]
    public sealed class ExceptionHandingContext
    {
        public ExecutionContext ExecutionContext { get; private set; }
        public int TryPointer { get; private set; }
        public int CatchPointer { get; private set; }
        public int FinallyPointer { get; private set; }
        public int EndPointer { get; private set; }
        public bool HasCatch { get; private set; }
        public bool HasFinally { get; private set; }
        public TryState State { get; internal set; } = TryState.Try;

        public ExceptionHandingContext(ExecutionContext ExecutionContext, int catchOffset, int finallyOffset)
        {
            this.ExecutionContext = ExecutionContext;
            this.TryPointer = ExecutionContext.InstructionPointer;
            this.HasCatch = catchOffset != 0;
            this.HasFinally = finallyOffset != 0;
            this.CatchPointer = checked(TryPointer + catchOffset);
            this.FinallyPointer = checked(TryPointer + finallyOffset);
        }

        public void EndTryCatch(int EndPointer)
        {
            this.EndPointer = EndPointer;
            this.State = TryState.Finally;
        }
    }
}
