using System;
using System.Collections.Generic;
using System.Text;

namespace neo_asm.ASM_Language
{

    public class ASMDocument:IASMNode
    {
        private ASMDocument()
        {
        }
        public IList<IASMNode> nodes => new List<IASMNode>();

        ASMDocument Parse(params string[] srcodes)
        {

            return null;
        }
    }
}
