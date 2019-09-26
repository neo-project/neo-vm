using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.ASML.Node
{
    public class ParsedSourceCode
    {

        public string filename
        {
            get;
            set;
        }
        public string srccode
        {
            get;
            set;
        }
        public IList<Word> words
        {
            get;
            set;
        }

        public struct Range
        {
            public ParsedSourceCode srccode;
            public int beginwordindex;
            public int endwordindex;
        }
    }

}
