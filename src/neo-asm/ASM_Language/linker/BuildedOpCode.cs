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

        //mapinfo
        public string srcfile;
        public int srcfileline;
        public int srcfilecol;
        public string srcInstructionComment;//comment in sourcecode
    }
}
