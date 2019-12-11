using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    [DebuggerDisplay("RVCount={RVCount}, InstructionPointer={InstructionPointer}")]
    public sealed class ExecutionContext
    {
        private readonly Dictionary<Type, object> states = new Dictionary<Type, object>();

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

        /// <summary>
        /// Alternative stack
        /// </summary>
        public EvaluationStack AltStack { get; }

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
            : this(script, rvcount, new EvaluationStack(referenceCounter), new EvaluationStack(referenceCounter))
        {
        }

        private ExecutionContext(Script script, int rvcount, EvaluationStack stack, EvaluationStack alt)
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

        public T GetState<T>()
        {
            return (T)states[typeof(T)];
        }

        public bool TryGetState<T>(out T value)
        {
            if (states.TryGetValue(typeof(T), out var val))
            {
                value = (T)val;
                return true;
            }

            value = default;
            return false;
        }

        public void SetState<T>(T state)
        {
            states[typeof(T)] = state;
        }

        internal bool MoveNext()
        {
            InstructionPointer += CurrentInstruction.Size;
            return InstructionPointer < Script.Length;
        }
    }
}
