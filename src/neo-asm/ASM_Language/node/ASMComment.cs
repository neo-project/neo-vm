using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.ASML.Node
{
    public class ASMComment : IASMNode
    {
        public IList<IASMNode> nodes => null;
        public string text;

        public SourceCode.Range srcmap
        {
            get;
            set;
        }
    }
}
