using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.Asm.Language
{
    public class Parser
    {
        public static ASMDocument Parse(params ParsedSourceCode[] srccodes)
        {
            ASMDocument doc = new ASMDocument();

            foreach (var src in srccodes)
            {
                ParseFile(doc, src);
            }

            return doc;
        }

        static void ParseFile(ASMDocument doc, ParsedSourceCode srccode)
        {
            if (doc.srccodes.ContainsKey(srccode.filename))
                throw new Exception("already have that." + srccode.filename);

            doc.srccodes[srccode.filename] = srccode;


            for (var i = 0; i < srccode.words.Count; i++)
            {
                var curword = srccode.words[i];
                if (curword.wordtype == Scanner.WordType.Comment)//comment
                {
                    ASMComment comment = new ASMComment() { text = curword.text, };
                    doc.nodes.Add(comment);
                }
                else if (curword.wordtype == Scanner.WordType.Word)
                {
                    var func = ParseWords(srccode, i);
                    i = func.srcmap.endwordindex;
                    doc.nodes.Add(func);
                }//maybe is a function
                else if (curword.wordtype == Scanner.WordType.NewLine || curword.wordtype == Scanner.WordType.Space)
                {
                    continue;
                }
                else
                {
                    throw new Exception("error parse format");
                }
            }
        }

        public static ASMFunction ParseWords(ParsedSourceCode srccode, int indexBegin)
        {
            var words = srccode.words;

            ASMFunction func = new ASMFunction();
            func.Name = words[indexBegin].text;

            var beginword = words[indexBegin];
            var beginParentheses = -1;
            var endParentheses = -1;
            var beginBraces = -1;
            var endBraces = -1;
            for (var i=indexBegin+1; i < words.Count; i++)
            {
                var curword = words[i];
                if (curword.wordtype == Scanner.WordType.Space || curword.wordtype == Scanner.WordType.NewLine)
                    continue;
                if (beginParentheses < 0)//find (
                {
                    if (curword.wordtype == Scanner.WordType.Parentheses && curword.text == "(")
                    {
                        beginParentheses = i;
                        continue;
                    }
                }
                else if (endParentheses < 0)// find )
                {
                    if (curword.wordtype == Scanner.WordType.Comment)
                    {
                        func.commentParams += curword.text;
                        continue;
                    }
                    if (curword.wordtype == Scanner.WordType.Parentheses && curword.text == ")")
                    {
                        endParentheses = i;
                        continue;
                    }
                }
                else if (beginBraces < 0)
                {
                    if (curword.wordtype == Scanner.WordType.Comment)
                    {
                        func.commentRight += curword.text;
                        continue;
                    }
                    if (curword.wordtype == Scanner.WordType.Braces && curword.text == "{")
                    {
                        beginBraces = i;
                        continue;
                    }
                }
                else if (endBraces < 0)
                {
                    if (curword.wordtype == Scanner.WordType.Braces && curword.text == "}")
                    {
                        endBraces = i;
                        func.srcmap = new ParsedSourceCode.Range() { srccode = srccode, beginwordindex = indexBegin, endwordindex = endBraces };
                        return func;
                    }
                    else
                    {
                        //填充函数内容
                        continue;
                    }
                }


                throw new Exception("unknown format.");
            }

            return null;
        }
    }
}
