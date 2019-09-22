using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.Asm.Language
{
    public interface IASMNode
    {
        IList<IASMNode> nodes
        {
            get;
        }
    }

}
