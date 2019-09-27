using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.ASML.Linker
{
    public class BuildedModule
    {
        public Dictionary<string, BuildedFunction> methods;
        public List<string> buildmethods;
        public byte[] getFinalBytes()
        {
            using (var ms = new System.IO.MemoryStream())
            {
                foreach (var m in buildmethods)
                {
                    var method = methods[m];
                    var bs = method.getFinalBytes();
                    ms.Write(bs, 0, bs.Length);
                }
                return ms.ToArray();
            }
        }
        public int getFinalLength()
        {
            var length = 0;
            foreach (var m in buildmethods)
            {
                var method = methods[m];
                length += method.getFinalLength();
            }
            return length;
        }
        public void Dump(Action<string> logaction)
        {
            logaction("==Dump Module");
            foreach(var func in methods.Values)
            {
                logaction(func.name + "()");
                logaction("{");
                foreach(var c in func.codes)
                {
                    logaction("    " + c);
                }

                logaction("}");

            }
               
        }
    }
}
