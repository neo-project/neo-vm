using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.ASML.Linker
{
    public class BuildedOpCode
    {
        public byte[] finalbytes;

        public int addr;

        public string JMPTarget;//JMP code,need convert addr in function.
        public string CALLTarget;//CALL code,need convert addr after link

        public string[] labels;
        //mapinfo
        public ASML.Node.ASMInstruction srcInstruction;

        public override string ToString()
        {
            var addrstr = addr.ToString("X04") + ":";

            if (labels != null && labels.Length > 0)
            {
                addrstr += "[";
                foreach (var l in labels)
                {
                    addrstr += l + ":";
                }
                addrstr += "]";

            }
            var str = addrstr + " " + ((Neo.VM.OpCode)finalbytes[0]).ToString();

            if (JMPTarget != null)
            {
                str += " [" + JMPTarget + "]";
            }
            else if (CALLTarget != null)
            {
                str += " [" + CALLTarget + "]";
            }
            if (finalbytes.Length > 1)
            {
                str += " data=[";
                for (var i = 1; i < finalbytes.Length; i++)
                {
                    str += finalbytes[i].ToString("X02");
                }
                str += "]";
            }
            return str;
        }
    }
}
