using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.Asm.Language
{
    public class ParsedSourceCode
    {
        public static ParsedSourceCode Parse(string filename,string srccode)
        {
            ParsedSourceCode code = new ParsedSourceCode();
            code.filename = filename;
            code.srccode = srccode;
            code.words = Scanner.Scan(srccode);

            return code;
        }
        public struct LineCol
        {
            public int line;
            public int col;
        }
        public string filename
        {
            get;
            private set;
        }
        public string srccode
        {
            get;
            private set;
        }
        public IList<Scanner.Word> words
        {
            get;
            private set;
        }

        public struct Range
        {
            public ParsedSourceCode srccode;
            public int beginwordindex;
            public int endwordindex;
        }
    }
 
}
