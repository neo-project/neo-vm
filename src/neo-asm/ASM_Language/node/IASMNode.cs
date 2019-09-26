using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.ASML.Node
{
    public interface IASMNode
    {
        IList<IASMNode> nodes
        {
            get;
        }
        ParsedSourceCode.Range srcmap
        {
            get;
        }

    }

}
