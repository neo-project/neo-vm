using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    [DebuggerDisplay("RVCount={RVCount}, InstructionPointer={InstructionPointer}")]
    public sealed class ExecutionContext
    {
        private readonly Dictionary<Type, object> states;

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
        public EvaluationStack EvaluationStack { get; }

        public Slot StaticFields { get; internal set; }

        public Slot LocalVariables { get; internal set; }

        public Slot Arguments { get; internal set; }

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
        internal ExecutionContext(Script script, int rvcount, ReferenceCounter referenceCounter)
            : this(script, rvcount, new EvaluationStack(referenceCounter), new Dictionary<Type, object>())
        {
        }

        private ExecutionContext(Script script, int rvcount, EvaluationStack stack, Dictionary<Type, object> states)
        {
            this.RVCount = rvcount;
            this.Script = script;
            this.EvaluationStack = stack;
            this.states = states;
        }

        internal ExecutionContext Clone()
        {
            return new ExecutionContext(Script, 0, EvaluationStack, states) { StaticFields = StaticFields };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Instruction GetInstruction(int ip) => Script.GetInstruction(ip);

        public T GetState<T>() where T : class, new()
        {
            if (!states.TryGetValue(typeof(T), out object value))
            {
                value = new T();
                states[typeof(T)] = value;
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
