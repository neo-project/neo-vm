using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.ASML.Linker
{

    public class BuildedFunction
    {
        public byte[] finalbytes;

        public string name;
        public List<BuildedOpCode> codes;
    }

}
