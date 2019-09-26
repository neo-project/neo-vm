using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.ASML.Node
{
    [System.Flags]
    public enum WordType
    {
        None = 0,
        Comment = 1,//注释
        Word = 2,//any text
        Space = 4,
        String = 8,//word with "" or ''
        Parentheses = 16,//( & )
        Braces = 32,//{ & }
        Colon = 64,//:
        NewLine = 128,//enter & ;
    }
}
