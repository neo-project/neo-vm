using System;
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
        public int TryPointer { get; }
        public int CatchPointer { get; }
        public int FinallyPointer { get; }

        public bool HasCatch => CatchPointer > TryPointer;
        public bool HasFinally => FinallyPointer > TryPointer;

        public TryState State { get; internal set; } = TryState.Try;

        public TryContent(int tryPointer, int catchOffset, int finallyOffset)
        {
            this.TryPointer = tryPointer;
            this.CatchPointer = checked(tryPointer + catchOffset);
            this.FinallyPointer = checked(tryPointer + finallyOffset);
        }
    }
}
