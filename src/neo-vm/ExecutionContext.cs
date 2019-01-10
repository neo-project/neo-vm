using System;
using System.IO;

namespace Neo.VM
{
    public class ExecutionContext : IDisposable
    {
        public readonly Script Script;
        internal readonly int RVCount;
        internal readonly BinaryReader OpReader;

        public RandomAccessStack<StackItem> EvaluationStack { get; } = new RandomAccessStack<StackItem>();
        public RandomAccessStack<StackItem> AltStack { get; } = new RandomAccessStack<StackItem>();

        public int InstructionPointer
        {
            get
            {
                return (int)OpReader.BaseStream.Position;
            }
            set
            {
                OpReader.BaseStream.Seek(value, SeekOrigin.Begin);
            }
        }

        public OpCode NextInstruction
        {
            get
            {
                var position = OpReader.BaseStream.Position;
                if (position >= Script.Length) return OpCode.RET;

                return (OpCode)Script.Value[position];
            }
        }

        public byte[] ScriptHash => Script.ScriptHash;

        internal ExecutionContext(Script script, int rvcount)
        {
            this.RVCount = rvcount;
            this.Script = script;
            this.OpReader = new BinaryReader(new MemoryStream(script.Value, false));
        }

        public void Dispose()
        {
            OpReader.Dispose();
        }
    }
}
