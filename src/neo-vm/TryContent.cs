using System;
using System.Diagnostics;

namespace Neo.VM
{
    [DebuggerDisplay("TryPointer={TryPointer}, CatchPointer={CatchPointer}, FinallyPointer={FinallyPointer}")]
    public class TryContent : IDisposable
    {
        public int TryPointer { get; }
        public int CatchPointer { get; }
        public int FinallyPointer { get; }

        public VMException RedirectionError { get; internal set; }

        /// <summary>
        /// Evaluation stack
        /// </summary>
        public EvaluationStack EvaluationStack { get; }

        public bool NeedToRet { get; internal set; }

        public TryContent(int tryPointer, int catchOffset, int finallyOffset, EvaluationStack evaluationStack)
        {
            this.TryPointer = tryPointer;
            this.CatchPointer = checked(tryPointer + catchOffset);
            this.FinallyPointer = checked(tryPointer + finallyOffset);
            this.EvaluationStack = evaluationStack;
        }

        public void Dispose()
        {
            EvaluationStack.Clear();
        }
    }
}
