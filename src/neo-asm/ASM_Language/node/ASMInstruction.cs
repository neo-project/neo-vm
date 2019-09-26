using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.ASML.Node
{
    public class ASMInstruction:IASMNode
    {

        public IList<IASMNode> nodes => null;
        /// <summary>
        /// OPCode
        /// </summary>
        public ASMOpCode opcode;

        /// <summary>
        /// valuetext.can be NULL
        /// </summary>
        public string valuetext;

        /// <summary>
        /// Comment
        /// </summary>
        public string commentRight;

        public SourceCode.Range srcmap
        {
            get;
            set;
        }
    }
}
