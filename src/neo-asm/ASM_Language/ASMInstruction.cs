using System;
using System.Collections.Generic;
using System.Text;

namespace neo_asm.ASM_Language
{
    public class ASMInstruction:IASMNode
    {
        public IList<IASMNode> nodes => null;

        /// <summary>
        /// instruction label,can be NULL
        /// </summary>
        string label;

        /// <summary>
        /// OPCode
        /// </summary>
        Neo.VM.OpCode opcode;

        /// <summary>
        /// valuetext.can be NULL
        /// </summary>
        string valuetext;

        /// <summary>
        /// Comment
        /// </summary>
        string commentRight;
    }
}
