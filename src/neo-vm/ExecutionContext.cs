using System;
using System.IO;

namespace Neo.VM
{
    public class ExecutionContext : IDisposable
    {
        private ExecutionEngine engine;
        public readonly byte[] Script;
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

        public OpCode NextInstruction => (OpCode)Script[OpReader.BaseStream.Position];

        private byte[] _script_hash = null;
        public byte[] ScriptHash
        {
            get
            {
                if (_script_hash == null)
                    _script_hash = engine.Crypto.Hash160(Script);
                return _script_hash;
            }
        }

        internal ExecutionContext(ExecutionEngine engine, byte[] script)
        {
            this.engine = engine;
            this.Script = script;
            this.OpReader = new BinaryReader(new MemoryStream(script, false));
        }

        public ExecutionContext Clone()
        {
            return new ExecutionContext(engine, Script)
            {
                InstructionPointer = InstructionPointer
            };
        }

        public void Dispose()
        {
            OpReader.Dispose();
        }
    }
}
