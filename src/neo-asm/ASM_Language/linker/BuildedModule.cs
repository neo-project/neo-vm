using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.ASML.Linker
{
    public class BuildedModule
    {
        public Dictionary<string, BuildedFunction> methods;

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
