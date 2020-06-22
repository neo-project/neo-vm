using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    [DebuggerDisplay("InstructionPointer={InstructionPointer}")]
    public sealed partial class ExecutionContext
    {
        private readonly SharedStates shared_states;

        /// <summary>
        /// Script
        /// </summary>
        public Script Script => shared_states.Script;

        /// <summary>
        /// Evaluation stack
        /// </summary>
        public EvaluationStack EvaluationStack => shared_states.EvaluationStack;

        public Slot StaticFields
        {
            get => shared_states.StaticFields;
            internal set => shared_states.StaticFields = value;
        }

        public Slot LocalVariables { get; internal set; }

        public Slot Arguments { get; internal set; }

        public Stack<ExceptionHandlingContext> TryStack { get; internal set; }

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
        /// <param name="rvcount">Number of items to be returned</param>
        internal ExecutionContext(Script script, ReferenceCounter referenceCounter)
            : this(new SharedStates(script, referenceCounter))
        {
        }

        private ExecutionContext(SharedStates shared_states)
        {
            this.shared_states = shared_states;
        }

        public ExecutionContext Clone()
        {
            return new ExecutionContext(shared_states);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Instruction GetInstruction(int ip) => Script.GetInstruction(ip);

        public T GetState<T>() where T : class, new()
        {
            if (!shared_states.States.TryGetValue(typeof(T), out object value))
            {
                value = new T();
                shared_states.States[typeof(T)] = value;
            }
            return (T)value;
        }

        internal bool MoveNext()
        {
            InstructionPointer += CurrentInstruction.Size;
            return InstructionPointer < Script.Length;
        }
    }
}
