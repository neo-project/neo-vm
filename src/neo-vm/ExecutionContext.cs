using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    /// <summary>
    /// Represents a frame in the VM execution stack.
    /// </summary>
    [DebuggerDisplay("InstructionPointer={InstructionPointer}")]
    public sealed partial class ExecutionContext
    {
        private readonly SharedStates shared_states;
        private int instructionPointer;

        /// <summary>
        /// Indicates the number of values that the context should return when it is unloaded.
        /// </summary>
        public int RVCount { get; }

        /// <summary>
        /// The script to run in this context.
        /// </summary>
        public Script Script => shared_states.Script;

        /// <summary>
        /// The evaluation stack for this context.
        /// </summary>
        public EvaluationStack EvaluationStack => shared_states.EvaluationStack;

        /// <summary>
        /// The slot used to store the static fields.
        /// </summary>
        public Slot StaticFields
        {
            get => shared_states.StaticFields;
            internal set => shared_states.StaticFields = value;
        }

        /// <summary>
        /// The slot used to store the local variables of the current method.
        /// </summary>
        public Slot LocalVariables { get; internal set; }

        /// <summary>
        /// The slot used to store the arguments of the current method.
        /// </summary>
        public Slot Arguments { get; internal set; }

        /// <summary>
        /// The stack containing nested <see cref="ExceptionHandlingContext"/>.
        /// </summary>
        public Stack<ExceptionHandlingContext> TryStack { get; internal set; }

        /// <summary>
        /// The pointer indicating the current instruction.
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

        /// <summary>
        /// Returns the current <see cref="Instruction"/>.
        /// </summary>
        public Instruction CurrentInstruction
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return GetInstruction(InstructionPointer);
            }
        }

        /// <summary>
        /// Returns the next <see cref="Instruction"/>.
        /// </summary>
        public Instruction NextInstruction
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return GetInstruction(InstructionPointer + CurrentInstruction.Size);
            }
        }

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

        /// <summary>
        /// Clones the context so that they share the same script, stack, and static fields.
        /// </summary>
        /// <returns>The cloned context.</returns>
        public ExecutionContext Clone()
        {
            return Clone(InstructionPointer);
        }

        /// <summary>
        /// Clones the context so that they share the same script, stack, and static fields.
        /// </summary>
        /// <param name="initialPosition">The instruction pointer of the new context.</param>
        /// <returns>The cloned context.</returns>
        public ExecutionContext Clone(int initialPosition)
        {
            return new ExecutionContext(shared_states, 0, initialPosition);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Instruction GetInstruction(int ip) => Script.GetInstruction(ip);

        /// <summary>
        /// Gets custom data of the specified type. If the data does not exist, create a new one.
        /// </summary>
        /// <typeparam name="T">The type of data to be obtained.</typeparam>
        /// <returns>The custom data of the specified type.</returns>
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
