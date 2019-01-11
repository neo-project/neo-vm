using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    public class ExecutionContext : IDisposable
    {
        /// <summary>
        /// Number of items to be returned
        /// </summary>
        internal readonly int RVCount;

        /// <summary>
        /// Binary Reader of the script
        /// </summary>
        internal readonly BinaryReader OpReader;

        /// <summary>
        /// Script
        /// </summary>
        public readonly Script Script;

        /// <summary>
        /// Evaluation stack
        /// </summary>
        public RandomAccessStack<StackItem> EvaluationStack { get; } = new RandomAccessStack<StackItem>();

        /// <summary>
        /// Alternative stack
        /// </summary>
        public RandomAccessStack<StackItem> AltStack { get; } = new RandomAccessStack<StackItem>();

        /// <summary>
        /// Instruction pointer
        /// </summary>
        public int InstructionPointer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (int)OpReader.BaseStream.Position;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                OpReader.BaseStream.Seek(value, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Next instruction
        /// </summary>
        public OpCode NextInstruction
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var position = (int)OpReader.BaseStream.Position;

                return position >= Script.Length ? OpCode.RET : Script[position];
            }
        }

        /// <summary>
        /// Cached script hash
        /// </summary>
        public byte[] ScriptHash
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Script.ScriptHash;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="script">Script</param>
        /// <param name="rvcount">Number of items to be returned</param>
        internal ExecutionContext(Script script, int rvcount)
        {
            this.RVCount = rvcount;
            this.Script = script;
            this.OpReader = script.GetBinaryReader();
        }

        /// <summary>
        /// Free resources
        /// </summary>
        public void Dispose() => OpReader.Dispose();
    }
}
