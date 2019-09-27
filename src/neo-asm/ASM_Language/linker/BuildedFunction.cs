using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.ASML.Linker
{

    public class BuildedFunction
    {
        public int addr;
        public string name;
        public List<BuildedOpCode> codes;

        public int getFinalLength()
        {
            if (codes.Count == 0)
                return 0;
            var lastcode = codes[codes.Count - 1];
            return lastcode.addr + lastcode.finalbytes.Length;
        }
        public byte[] getFinalBytes()
        {
            using (var ms = new System.IO.MemoryStream())
            {
                foreach (var c in codes)
                {
                    ms.Write(c.finalbytes, 0, c.finalbytes.Length);
                }
                return ms.ToArray();
            }
        }
    }

}
