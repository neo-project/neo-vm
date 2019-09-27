using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.ASML.Linker
{
    public class BuildedModule
    {
        public Dictionary<string, BuildedFunction> functions;
        public List<string> buildfuncs;
        public byte[] getFinalBytes()
        {
            using (var ms = new System.IO.MemoryStream())
            {
                foreach (var m in buildfuncs)
                {
                    var func = functions[m];
                    var bs = func.getFinalBytes();
                    ms.Write(bs, 0, bs.Length);
                }
                return ms.ToArray();
            }
        }
        public int getFinalLength()
        {
            var length = 0;
            foreach (var m in buildfuncs)
            {
                var func = functions[m];
                length += func.getFinalLength();
            }
            return length;
        }
        public void Dump(Action<string> logaction)
        {
            logaction("==Dump Module");
            foreach (var func in functions.Values)
            {
                logaction(func.addr.ToString("X04") + ":" + func.name + "()");
                logaction("{");
                foreach (var c in func.codes)
                {
                    logaction("    " + c);
                }

                logaction("}");

            }

        }

        public JObject genDebugInfo()
        {
            var jobj = new JObject();
            List<string> srcfiles = new List<string>();
            var jfuncs = new JArray();
            jobj["functions"] = jfuncs;
            foreach (var m in buildfuncs)
            {
                var func = functions[m];
                var jfuncobj = new JObject();
                jfuncs.Add(jfuncobj);
                jfuncobj["name"] = func.name;
                jfuncobj["addr"] = func.addr;
                jfuncobj["bytelength"] = func.getFinalLength();
                var jcodesobj = new JArray();
                jfuncobj["codes"] = jcodesobj;
                foreach (var c in func.codes)
                {
                    var codeitemObj = new JObject();
                    jcodesobj.Add(codeitemObj);
                    codeitemObj["addr"] = c.addr;
                    codeitemObj["code"] = ((Neo.VM.OpCode)c.finalbytes[0]).ToString();
                    if (c.labels != null && c.labels.Length > 0)
                    {
                        codeitemObj["labels"] = new JArray(c.labels);
                    }
                    if (c.JMPTarget != null)
                        codeitemObj["jmp"] = c.JMPTarget;
                    if (c.CALLTarget != null)
                        codeitemObj["call"] = c.CALLTarget;

                    if (c.srcInstruction.commentRight != null)
                    {
                        codeitemObj["comment"] = c.srcInstruction.commentRight;
                    }
                    var srcmap = c.srcInstruction.srcmap;
                    var srcfile = srcmap.srccode.filename;
                    if (srcfiles.Contains(srcfile) == false)
                    {
                        srcfiles.Add(srcfile);
                    }

                    //one function one srcfile
                    //if (jfuncobj.ContainsKey("srcfile")==false)
                    //{
                    //    jfuncobj["srcfileindex"] = srcfiles.IndexOf(srcfile);
                    //}

                    //one code one srcfile
                    codeitemObj["srcfile"] = srcfiles.IndexOf(srcfile);
                    var beginline = srcmap.srccode.words[srcmap.beginwordindex].line;
                    var begincol = srcmap.srccode.words[srcmap.beginwordindex].col;

                    var line = (beginline + 1) + "," + (begincol + 1);
                    if (srcmap.endwordindex != srcmap.beginwordindex)
                    {
                        var endline = srcmap.srccode.words[srcmap.endwordindex].line;
                        var endcol = srcmap.srccode.words[srcmap.endwordindex].col;
                        line += "-" + (endline + 1) + "," + (endcol + 1);
                    }
                    codeitemObj["srcpos"] = line;

                }
            }
            var jsrcs = new JArray(srcfiles);
            jobj["srcfiles"] = jsrcs;
            return jobj;
        }
    }
}
