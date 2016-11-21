using System;
using System.Collections.Generic;
using System.IO;

namespace AntShares.VM
{
    public class ScriptContext : IDisposable
    {
        public readonly byte[] Script;
        internal readonly bool PushOnly;
        internal readonly BinaryReader OpReader;
        internal readonly HashSet<uint> BreakPoints = new HashSet<uint>();

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

        internal ScriptContext(byte[] script, bool push_only)
        {
            this.Script = script;
            this.PushOnly = push_only;
            this.OpReader = new BinaryReader(new MemoryStream(script, false));
        }

        public void Dispose()
        {
            OpReader.Dispose();
        }
    }
}
