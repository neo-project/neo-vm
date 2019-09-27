using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.ASML.Linker
{
    public class BuildedOpCode
    {
        public byte[] finalbytes;

        public int addr;

        public bool isJMP;//JMP code,need convert addr in function.
        public bool isCALL;//CALL code,need convert addr after link

        public string[] labels;
        //mapinfo
        public ASML.Node.ASMInstruction srcInstruction;

        public override string ToString()
        {
            var str = addr.ToString("X04")+": "+ ((Neo.VM.OpCode)finalbytes[0]).ToString();
            if (finalbytes.Length > 1)
                str += " datalen=" + (finalbytes.Length - 1);
            return str;
        }
    }
}
