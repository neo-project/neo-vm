using System;
using System.Collections.Generic;
using System.IO;

namespace AntShares.VM
{
    internal class ScriptContext : IDisposable
    {
        public byte[] Script;
        public BinaryReader OpReader;
        public HashSet<uint> BreakPoints = new HashSet<uint>();

        public ScriptContext(byte[] script)
        {
            this.Script = script;
            this.OpReader = new BinaryReader(new MemoryStream(script, false));
        }

        public void Dispose()
        {
            OpReader.Dispose();
        }
    }
}
