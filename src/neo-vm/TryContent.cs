using Neo.VM.Types;
using System.Diagnostics;

namespace Neo.VM
{
    public enum TryState : byte
    {
        Try,
        Catch,
        Finally
    }

    [DebuggerDisplay("TryPointer={TryPointer}, CatchPointer={CatchPointer}, FinallyPointer={FinallyPointer}")]
    public sealed class TryContent
    {
        public int TryPointer { get; private set; }
        public int CatchPointer { get; private set; }
        public int FinallyPointer { get; private set; }
        public int EvaluationStackCount { get; private set; }

        public int EndPointer { get; private set; }

        public void EndTryCatch(int EndPointer)
        {
            this.EndPointer = EndPointer;
            this.State = TryState.Finally;
        }
        public bool HasCatch { get; private set; }
        public bool HasFinally { get; private set; }

        public TryState State { get; internal set; } = TryState.Try;
        public StackItem CatchedError { get; internal set; }
        public StackItem RethrowError { get; internal set; }

        public TryContent(int tryPointer, int EvaluationStackCount, int catchOffset, int finallyOffset)
        {
            this.TryPointer = tryPointer;
            this.EvaluationStackCount = EvaluationStackCount;
            HasCatch = catchOffset > 0;
            HasFinally = finallyOffset > 0;
            this.CatchPointer = checked(tryPointer + catchOffset);
            this.FinallyPointer = checked(tryPointer + finallyOffset);
        }
    }
}
