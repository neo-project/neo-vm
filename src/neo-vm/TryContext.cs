using System.Diagnostics;

namespace Neo.VM
{
    [DebuggerDisplay("State={State}, TryPointer={TryPointer}, CatchPointer={CatchPointer}, FinallyPointer={FinallyPointer}, EndPointer={EndPointer}")]
    public sealed class TryContext
    {
        public ExecutionContext ExecutionContext { get; private set; }
        public int TryPointer { get; private set; }
        public int CatchPointer { get; private set; }
        public int FinallyPointer { get; private set; }
        public bool HasCatch { get; private set; }
        public bool HasFinally { get; private set; }
        public TryState State { get; internal set; } = TryState.Try;

        public TryContext(ExecutionContext ExecutionContext, int catchOffset, int finallyOffset)
        {
            this.ExecutionContext = ExecutionContext;
            this.TryPointer = ExecutionContext.InstructionPointer;
            this.HasCatch = catchOffset != 0;
            this.HasFinally = finallyOffset != 0;
            this.CatchPointer = checked(TryPointer + catchOffset);
            this.FinallyPointer = checked(TryPointer + finallyOffset);
        }
    }
}
