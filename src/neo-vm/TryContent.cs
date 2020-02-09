using System;
using System.Diagnostics;

namespace Neo.VM
{
    [DebuggerDisplay("TryPointer={TryPointer}, CatchPointer={CatchPointer}, FinallyPointer={FinallyPointer}, EndPointer={EndPointer}")]
    public sealed class TryContent
    {
        public int TryPointer { get; }
        public int CatchPointer { get; }
        public int FinallyPointer { get; }
        public int EndPointer { get; }
        public EvaluationStack EvaluationStack { get; }

        public bool HasCatch => CatchPointer > TryPointer;
        public bool HasFinally => FinallyPointer > TryPointer;

        public Tuple<OpCode, object> PostExecuteOpcode { get; internal set; }

        public TryContent(int tryPointer, int catchOffset, int finallyOffset, int endOffset, EvaluationStack evaluationStack)
        {
            this.TryPointer = tryPointer;
            this.CatchPointer = checked(tryPointer + catchOffset);
            this.FinallyPointer = checked(tryPointer + finallyOffset);
            this.EndPointer = checked(tryPointer + endOffset);
            this.EvaluationStack = evaluationStack;
        }
    }
}
