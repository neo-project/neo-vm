using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    [DebuggerDisplay("RVCount={RVCount}, InstructionPointer={InstructionPointer}")]
    public class ExecutionContext
    {
        /// <summary>
        /// Number of items to be returned
        /// </summary>
        internal protected int RVCount { get; }

        /// <summary>
        /// Script
        /// </summary>
        public Script Script { get; }

        /// <summary>
        /// Evaluation stack
        /// </summary>
        public RandomAccessStack<StackItem> EvaluationStack { get; }

        /// <summary>
        /// Alternative stack
        /// </summary>
        public RandomAccessStack<StackItem> AltStack { get; }

        /// <summary>
        /// Instruction pointer
        /// </summary>
        public int InstructionPointer { get; set; }

        public Instruction CurrentInstruction
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return GetInstruction(InstructionPointer);
            }
        }

        /// <summary>
        /// Next instruction
        /// </summary>
        public Instruction NextInstruction
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return GetInstruction(InstructionPointer + CurrentInstruction.Size);
            }
        }

        public Script CallingScript { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="script">Script</param>
        /// <param name="callingScript">The calling script</param>
        /// <param name="rvcount">Number of items to be returned</param>
        internal protected ExecutionContext(Script script, Script callingScript, int rvcount)
            : this(script, callingScript, rvcount, new RandomAccessStack<StackItem>(), new RandomAccessStack<StackItem>())
        {
        }

        protected ExecutionContext(Script script, Script callingScript, int rvcount, RandomAccessStack<StackItem> stack, RandomAccessStack<StackItem> alt)
        {
            this.RVCount = rvcount;
            this.Script = script;
            this.EvaluationStack = stack;
            this.AltStack = alt;
            this.CallingScript = callingScript;
        }

        internal virtual ExecutionContext Clone()
        {
            return new ExecutionContext(Script, Script, 0, EvaluationStack, AltStack);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Instruction GetInstruction(int ip) => Script.GetInstruction(ip);

        internal protected bool MoveNext()
        {
            InstructionPointer += CurrentInstruction.Size;
            return InstructionPointer < Script.Length;
        }
    }
}
