using System;
using System.Collections.Generic;
using System.Text;

namespace neo_asm.ASM_Language
{
    public class ASMFunction:IASMNode
    {
        public IList<IASMNode> nodes => new List<IASMNode>();

        public string Name;

        public string commentParams;
        public string commentRight;
    }
}
