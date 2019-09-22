using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.Asm.Language
{

    public class ASMDocument : IASMNode
    {
        public ASMDocument()
        {
            this.nodes = new List<IASMNode>();
            this.srccodes = new Dictionary<string, ParsedSourceCode>();
        }
        public IList<IASMNode> nodes
        {
            get;
            private set;
        }
        public Dictionary<string, ParsedSourceCode> srccodes
        {
            get;
            private set;
        }
        public void Dump(Action<string> logaction)
        {
            logaction("asm docments:");
            logaction("[");
            foreach (var n in nodes)
            {
                if (n is ASMComment)
                {
                    ASMComment comment = n as ASMComment;
                    logaction("    <ASMComment>" + comment.text);
                }
                if (n is ASMFunction)
                {
                    ASMFunction func = n as ASMFunction;
                    logaction("    <ASMFunction>" + func.Name + "(" + func.commentParams + ")" + func.commentRight);
                    logaction("    {");
                    foreach (var fn in func.nodes)
                    {
                        if (fn is ASMComment)
                        {
                            ASMComment comment = fn as ASMComment;
                            logaction("        <ASMComment>" + comment.text);
                        }
                        if (fn is ASMInstruction)
                        {
                            ASMInstruction inst = fn as ASMInstruction;
                            logaction("        <ASMInstruction>" + inst.opcode.ToString() + " " + inst.valuetext);

                        }
                    }
                    logaction("    }");

                }
            }
            logaction("]");
        }
    }
}
