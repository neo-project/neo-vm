using Neo.ASML.Node;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.ASML.Parser
{
    public class WordScanner
    {
        const string controlChars = ";\n/* \t(){}:;'\"";

        public static IList<Word> Scan(string srctext)
        {
            int line = 0;
            int linebegin = 0;
            List<Word> words = new List<Word>();

            var text = srctext.Replace("\r", ""); //remove \r,only parse\n

            char lastchar = (char)0;
            for (var i = 0; i < text.Length; i++)
            {
                char curchar = text[i];
                if (controlChars.Contains(curchar))

                {
                    if (curchar == '\n')//newline
                    {
                        lastchar = (char)0;
                        var wordNewLine = new Word() { wordtype = WordType.NewLine, text = null, line = line, col = i - linebegin };
                        words.Add(wordNewLine);
                        line++;
                        linebegin = i + 1;
                        continue;
                    }
                    else if (curchar == ';')
                    {
                        lastchar = (char)0;
                        var wordNewLine = new Word() { wordtype = WordType.NewLine, text = ";", line = line, col = i - linebegin };
                        words.Add(wordNewLine);
                        continue;
                    }
                    else if (curchar == '/')//comment //
                    {
                        if (lastchar == '/')//后行注释
                        {
                            var jend = text.IndexOf("\n", i);
                            if (jend < 0) jend = text.Length - 1;
                            var alltext = text.Substring(i - 1, jend - i + 1);
                            var wordComment = new Word() { wordtype = WordType.Comment, text = alltext, line = line, col = i - linebegin - 1 };
                            words.Add(wordComment);
                            i = jend - 1;
                            lastchar = (char)0;
                            continue;
                        }
                        else
                        {
                            lastchar = curchar;
                            continue;
                        }
                    }
                    else if (curchar == '*')//   /*
                    {
                        if (lastchar == '/')
                        {
                            var jend = text.IndexOf("*/", i);
                            if (jend < 0)
                                throw new Exception("error /* not match a */");
                            var alltext = text.Substring(i - 1, jend - i + 3);
                            var wordComment = new Word() { wordtype = WordType.Comment, text = alltext, line = line, col = i - linebegin - 1 };
                            words.Add(wordComment);
                            i = jend + 1;
                            lastchar = (char)0;
                            continue;
                        }
                        else
                        {
                            lastchar = curchar;
                            continue;
                        }
                    }
                    else if (curchar == ' ' || curchar == '\t')
                    {
                        var jend = i;
                        for (; jend < text.Length; jend++)
                        {
                            if (text[jend] != curchar)
                                break;
                        }
                        var wordSpace = new Word() { wordtype = WordType.Space, text = curchar.ToString(), line = line, col = i - linebegin };
                        words.Add(wordSpace);

                        i = jend - 1;
                        lastchar = (char)0;
                        continue;
                    }
                    else if (curchar == '(' || curchar == ')')
                    {
                        var word = new Word() { wordtype = WordType.Parentheses, text = curchar.ToString(), line = line, col = i - linebegin };
                        words.Add(word);
                        lastchar = (char)0;
                        continue;
                    }
                    else if (curchar == '{' || curchar == '}')
                    {
                        var word = new Word() { wordtype = WordType.Braces, text = curchar.ToString(), line = line, col = i - linebegin };
                        words.Add(word);
                        lastchar = (char)0;
                        continue;
                    }
                    else if (curchar == ':')
                    {
                        var word = new Word() { wordtype = WordType.Colon, text = curchar.ToString(), line = line, col = i - linebegin };
                        words.Add(word);
                        lastchar = (char)0;
                        continue;
                    }
                    else if (curchar == '"' || curchar == '\'')//string
                    {
                        var jend = text.IndexOf(curchar, i + 1);
                        if (jend < 0)
                            throw new Exception("error string format.");
                        var alltext = text.Substring(i, jend - i + 1);
                        var word = new Word() { wordtype = WordType.String, text = alltext, line = line, col = i - linebegin };
                        words.Add(word);
                        i = jend;
                        lastchar = (char)0;
                        continue;
                    }
                }
                else
                {      //words
                    string wordstr = "";
                    var jend = i;
                    for (; jend < text.Length; jend++)
                    {
                        var nextchar = text[jend];
                        if (controlChars.Contains(nextchar))
                            break;
                        wordstr += nextchar;
                    }
                    var word = new Word() { wordtype = WordType.Word, text = wordstr, line = line, col = i - linebegin };
                    words.Add(word);
                    lastchar = (char)0;
                    i = jend - 1;
                    continue;
                }
            }

            return words;
        }


        public static SourceCode CreateSourceCode(string filename, string srccode)
        {
            SourceCode code = new SourceCode();
            code.filename = filename;
            code.srccode = srccode;
            code.words = WordScanner.Scan(srccode);

            return code;
        }
    }
}
