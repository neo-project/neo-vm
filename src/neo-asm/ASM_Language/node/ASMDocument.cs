using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.ASML.Node
{

    public class ASMDocument
    {
        public ASMDocument()
        {
            this.nodes = new List<IASMNode>();
            this.srccodes = new Dictionary<string, SourceCode>();
        }
        public IList<IASMNode> nodes
        {
            get;
            private set;
        }
        public Dictionary<string, SourceCode> srccodes
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
                        if (fn is ASMLabel)
                        {
                            ASMLabel label = fn as ASMLabel;
                            logaction("        <ASMLabel>" + label.label + "    " + label.commentRight);
                        }
                        if (fn is ASMInstruction)
                        {
                            ASMInstruction inst = fn as ASMInstruction;
                            logaction("        <ASMInstruction>" + inst.opcode.ToString() + " " + inst.valuetext + "   " + inst.commentRight);
                        }
                    }
                    logaction("    }");

                }
            }
            logaction("]");
        }
    }
}
