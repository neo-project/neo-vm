using System;
using System.Collections.Generic;
using System.Text;

namespace neo_asm.ASM_Language
{
    public class ASMComment : IASMNode
    {
        public IList<IASMNode> nodes => null;
        public string text;
    }
}
