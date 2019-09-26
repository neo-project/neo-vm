using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.ASML.Node
{
    public class Word
    {
        public WordType wordtype;
        public string text;
        public int line;
        public int col;
        public override string ToString()
        {
            var valuestr = "";
            if (text == null)
            {
                if (wordtype == WordType.NewLine)
                {
                    valuestr = "<ENTER>";
                }
                else
                {
                    valuestr = "<NULL>";
                }
            }
            if (wordtype == WordType.Space)
            {
                if (text[0] == ' ')
                {
                    valuestr = "<SPACE>";
                }
                else if (text[0] == '\t')
                {
                    valuestr = "<TAB>";
                }
            }
            else
            {
                valuestr = text;
            }
            return "<" + wordtype + "> " + valuestr + " (" + line + "," + col + ")";
        }
    }
}
