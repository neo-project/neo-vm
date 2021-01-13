using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    [DebuggerDisplay("InstructionPointer={InstructionPointer}")]
    public sealed partial class ExecutionContext
    {
        private readonly SharedStates shared_states;
        private int instructionPointer;

        /// <summary>
        /// Number of items to be returned
        /// </summary>
        public int RVCount { get; }

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
        public int InstructionPointer
        {
            get
            {
                return instructionPointer;
            }
            internal set
            {
                if (value < 0 || value > Script.Length)
                    throw new ArgumentOutOfRangeException(nameof(value));
                instructionPointer = value;
            }
        }

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
        internal ExecutionContext(Script script, int rvcount, ReferenceCounter referenceCounter)
            : this(new SharedStates(script, referenceCounter), rvcount, 0)
        {
        }

        private ExecutionContext(SharedStates shared_states, int rvcount, int initialPosition)
        {
            if (rvcount < -1 || rvcount > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(rvcount));
            this.shared_states = shared_states;
            this.RVCount = rvcount;
            this.InstructionPointer = initialPosition;
        }

        public ExecutionContext Clone()
        {
            return Clone(InstructionPointer);
        }

        public ExecutionContext Clone(int initialPosition)
        {
            return new ExecutionContext(shared_states, 0, initialPosition);
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
