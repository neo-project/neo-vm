using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.ASML.Node
{
    public class ASMLabel : IASMNode
    {
        public IList<IASMNode> nodes => null;
        public string label;
        public string commentRight;

        public SourceCode.Range srcmap
        {
            get;
            set;
        }
    }
}
