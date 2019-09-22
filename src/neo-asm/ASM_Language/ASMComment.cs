using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.Asm.Language
{
    public class ASMComment : IASMNode
    {
        public IList<IASMNode> nodes => null;
        public string text;

        public ParsedSourceCode.Range srcmap;
    }
}
