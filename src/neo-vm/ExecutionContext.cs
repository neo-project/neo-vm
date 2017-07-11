using System;
using System.Collections.Generic;
using System.IO;

namespace Neo.VM
{
    public class ExecutionContext : IDisposable
    {
        private ExecutionEngine engine;
        public readonly byte[] Script;
        public readonly bool PushOnly;
        internal readonly BinaryReader OpReader;
        internal readonly HashSet<uint> BreakPoints;

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

        internal ExecutionContext(ExecutionEngine engine, byte[] script, bool push_only, HashSet<uint> break_points = null)
        {
            this.engine = engine;
            this.Script = script;
            this.PushOnly = push_only;
            this.OpReader = new BinaryReader(new MemoryStream(script, false));
            this.BreakPoints = break_points ?? new HashSet<uint>();
        }

        public ExecutionContext Clone()
        {
            return new ExecutionContext(engine, Script, PushOnly, BreakPoints)
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
