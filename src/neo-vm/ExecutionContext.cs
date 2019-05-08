using System.Runtime.CompilerServices;

namespace Neo.VM
{
    public class ExecutionContext
    {
        /// <summary>
        /// Number of items to be returned
        /// </summary>
        internal int RVCount { get; }

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

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="script">Script</param>
        /// <param name="callingScriptHash">Script hash of the calling script</param>
        /// <param name="rvcount">Number of items to be returned</param>
        internal ExecutionContext(Script script, int rvcount)
            : this(script, rvcount, new RandomAccessStack<StackItem>(), new RandomAccessStack<StackItem>())
        {
        }

        private ExecutionContext(Script script, int rvcount, RandomAccessStack<StackItem> stack, RandomAccessStack<StackItem> alt)
        {
            this.RVCount = rvcount;
            this.Script = script;
            this.EvaluationStack = stack;
            this.AltStack = alt;
        }

        internal ExecutionContext Clone()
        {
            return new ExecutionContext(Script, 0, EvaluationStack, AltStack);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Instruction GetInstruction(int ip) => Script.GetInstruction(ip);

        internal bool MoveNext()
        {
            InstructionPointer += CurrentInstruction.Size;
            return InstructionPointer < Script.Length;
        }
    }
}
