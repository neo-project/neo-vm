using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.Asm.Language
{
    public class ASMLabel : IASMNode
    {
        public IList<IASMNode> nodes => null;
        public string label;
        public string commentRight;

        public ParsedSourceCode.Range srcmap
        {
            get;
            set;
        }
    }
}
