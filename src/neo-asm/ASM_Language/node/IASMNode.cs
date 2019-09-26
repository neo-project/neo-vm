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
        SourceCode.Range srcmap
        {
            get;
        }

    }

}
