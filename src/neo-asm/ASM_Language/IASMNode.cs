using System;
using System.Collections.Generic;
using System.Text;

namespace neo_asm.ASM_Language
{
    public interface IASMNode
    {
        IList<IASMNode> nodes
        {
            get;
        }
    }

}
