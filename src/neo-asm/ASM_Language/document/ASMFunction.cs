using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.Asm.Language
{ 
    public class ASMFunction:IASMNode
    {
        public ASMFunction()
        {
            this.nodes = new List<IASMNode>();
        }
        public IList<IASMNode> nodes
        {
            get;
            private set;
        }
        public string Name;

        public string commentParams;
        public string commentRight;

        public ParsedSourceCode.Range srcmap
        {
            get;
            set;
        }

    }
}
