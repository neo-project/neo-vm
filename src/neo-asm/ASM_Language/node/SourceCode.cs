using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.ASML.Node
{
    public class SourceCode
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
            public SourceCode srccode;
            public int beginwordindex;
            public int endwordindex;
        }
        public void DumpWords(Action<string> logaction)
        {
            logaction("==DUMP SourceCode words");
            foreach (var w in words)
            {
                logaction(w.ToString());
            }

        }
    }

}
