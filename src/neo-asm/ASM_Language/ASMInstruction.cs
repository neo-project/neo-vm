using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.Asm.Language
{
    public class ASMInstruction:IASMNode
    {
        public IList<IASMNode> nodes => null;

        /// <summary>
        /// instruction label,can be NULL
        /// </summary>
        public string label;

        /// <summary>
        /// OPCode
        /// </summary>
        public Neo.VM.OpCode opcode;

        /// <summary>
        /// valuetext.can be NULL
        /// </summary>
        public string valuetext;

        /// <summary>
        /// Comment
        /// </summary>
        public string commentRight;

        public ParsedSourceCode.Range srcmap;
    }
}
