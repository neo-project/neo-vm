using Neo.ASML.Node;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.ASML.Parser
{
    public class Parser
    {
        public static ASMProject Parse(params SourceCode[] srccodes)
        {
            ASMProject doc = new ASMProject();

            foreach (var src in srccodes)
            {
                ParseOne(doc, src);
            }

            return doc;
        }

        static void ParseOne(ASMProject doc, SourceCode srccode)
        {
            if (doc.srccodes.ContainsKey(srccode.filename))
                throw new Exception("already have that." + srccode.filename);

            doc.srccodes[srccode.filename] = srccode;


            for (var i = 0; i < srccode.words.Count; i++)
            {
                var curword = srccode.words[i];
                if (curword.wordtype == WordType.Comment)//comment
                {
                    ASMComment comment = new ASMComment() { text = curword.text, };
                    doc.nodes.Add(comment);
                }
                else if (curword.wordtype == WordType.Word)
                {
                    var func = ParseFunction(srccode, i);
                    i = func.srcmap.endwordindex;
                    doc.nodes.Add(func);
                }//maybe is a function
                else if (curword.wordtype == WordType.NewLine || curword.wordtype == WordType.Space)
                {
                    continue;
                }
                else
                {
                    throw new Exception("error parse format");
                }
            }
        }
        static Word FindNextWord(IList<Word> words, int indexBegin, WordType skiptypes = WordType.Space, WordType endtypes = WordType.NewLine)
        {
            for (var i = indexBegin; i < words.Count; i++)
            {
                var curword = words[i];
                if ((curword.wordtype & skiptypes) > 0)
                {
                    continue;
                }
                else if ((curword.wordtype & endtypes) > 0)
                {
                    return null;
                }
                else
                {
                    return curword;
                }
            }
            return null;
        }
        static ASMFunction ParseFunction(SourceCode srccode, int indexBegin)
        {
            var words = srccode.words;

            ASMFunction func = new ASMFunction();
            func.Name = words[indexBegin].text;

            var beginword = words[indexBegin];
            var beginParentheses = -1;
            var endParentheses = -1;
            var beginBraces = -1;
            var endBraces = -1;
            for (var i = indexBegin + 1; i < words.Count; i++)
            {
                var curword = words[i];
                if (curword.wordtype == WordType.Space || curword.wordtype == WordType.NewLine)
                    continue;
                if (beginParentheses < 0)//find (
                {
                    if (curword.wordtype == WordType.Parentheses && curword.text == "(")
                    {
                        beginParentheses = i;
                        continue;
                    }
                }
                else if (endParentheses < 0)// find )
                {
                    if (curword.wordtype == WordType.Comment)
                    {
                        func.commentParams += curword.text;
                        continue;
                    }
                    if (curword.wordtype == WordType.Parentheses && curword.text == ")")
                    {
                        endParentheses = i;
                        continue;
                    }
                }
                else if (beginBraces < 0)
                {
                    if (curword.wordtype == WordType.Comment)
                    {
                        func.commentRight += curword.text;
                        continue;
                    }
                    if (curword.wordtype == WordType.Braces && curword.text == "{")
                    {
                        beginBraces = i;
                        continue;
                    }
                }
                else if (endBraces < 0)
                {
                    if (curword.wordtype == WordType.Braces && curword.text == "}")
                    {
                        endBraces = i;
                        func.srcmap = new SourceCode.Range() { srccode = srccode, beginwordindex = indexBegin, endwordindex = endBraces };
                        return func;
                    }
                    else
                    {
                        if (curword.wordtype == WordType.Comment)
                        {
                            var comment = new ASMComment() { text = curword.text };
                            comment.srcmap = new SourceCode.Range() { srccode = srccode, beginwordindex = i, endwordindex = i };
                            func.nodes.Add(comment);
                            continue;
                        }
                        else if (curword.wordtype == WordType.NewLine || curword.wordtype == WordType.Space)
                        {
                            continue;
                        }
                        else if (curword.wordtype == WordType.Word)
                        {
                            //有可能是指令或者标签指令
                            var next = FindNextWord(words, i + 1);
                            if (next != null && next.wordtype == WordType.Colon)
                            {
                                var label = ParseLabel(srccode, i);
                                func.nodes.Add(label);
                                i = label.srcmap.endwordindex;
                                continue;

                            }
                            else
                            {
                                var inst = ParseInstruction(srccode, i);
                                func.nodes.Add(inst);
                                i = inst.srcmap.endwordindex;
                                continue;
                            }
                        }
                        else
                        {
                            throw new Exception("unknown format.");
                        }
                    }
                }


            }

            return null;
        }
        static ASMLabel ParseLabel(SourceCode srccode, int indexBegin)
        {
            ASMLabel label = new ASMLabel() { label = srccode.words[indexBegin].text };
            var next = FindNextWord(srccode.words, indexBegin + 2);
            if (next != null && next.wordtype == WordType.Comment)
            {
                //with comment;
                label.commentRight = next.text;
                label.srcmap = new SourceCode.Range()
                {
                    srccode = srccode,
                    beginwordindex = indexBegin,
                    endwordindex = srccode.words.IndexOf(next)
                };
            }
            else
            {
                label.commentRight = null;
                label.srcmap = new SourceCode.Range()
                {
                    srccode = srccode,
                    beginwordindex = indexBegin,
                    endwordindex = indexBegin + 1
                };
            }
            return label;
        }

        static ASMInstruction ParseInstruction(SourceCode srccode, int indexBegin)
        {
            ASMInstruction inst = null;
            var words = srccode.words;
            var curword = words[indexBegin];

            var next = FindNextWord(words, indexBegin + 1);

            int endindex = -1;
            string comment = null;
            string value = null;
            next = FindNextWord(words, words.IndexOf(curword) + 1);
            if (next == null)//op code with nothing
            {
                comment = null;
                value = null;
                endindex = words.IndexOf(curword);
            }
            else if (next.wordtype == WordType.Comment)
            {//op cpde with comment
                comment = next.text;
                value = null;
                endindex = words.IndexOf(next);
            }
            else if (next.wordtype == WordType.Word || next.wordtype == WordType.String)
            {//op code with param
                value = next.text;
                var commentnext = FindNextWord(words, words.IndexOf(next) + 1);
                if (commentnext != null && commentnext.wordtype == WordType.Comment)
                {
                    comment = commentnext.text;
                    endindex = words.IndexOf(commentnext);
                }
                else
                {
                    comment = null;
                    endindex = words.IndexOf(next);
                }
            }
            var code = ASMOpCode.Parse(curword.text);
            //this is a code
            inst = new ASMInstruction() { opcode = code, valuetext = value, commentRight = comment };
            inst.srcmap = new SourceCode.Range()
            {
                srccode = srccode,
                beginwordindex = indexBegin,
                endwordindex = endindex,

            };
            return inst;

        }
    }
}
