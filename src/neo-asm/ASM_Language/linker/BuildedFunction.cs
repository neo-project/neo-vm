using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.ASML.Linker
{

    public class BuildedFunction
    {
        public byte[] finalbytes;

        public string name;
        Dictionary<string, BuildedOpCode> label2code;
        List<BuildedOpCode> codes;
    }

}
