using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace neo_asm.Tests.Parser
{
    [TestClass]
    public class WordSplit
    {
        [TestMethod]
        public void TestBaseSplit()
        {
            const string testcode = @"
/*comment1*///comment 2
Main()
{
  PUSH 1
  PUSH 2
  ADD
  RET
}
";

            var code = Neo.ASML.Parser.WordScanner.CreateSourceCode("a.asm", testcode);
            code.DumpWords((str) => Console.WriteLine(str));
            //check output

        }
        [TestMethod]
        public void TestFunctionComment()
        {
            const string testcode = @"
/*comment1*///comment 2
Main(/*param comment*/)//rightComment
{
  PUSH 1
  PUSH 2
  ADD
  RET
}
";

            var code = Neo.ASML.Parser.WordScanner.CreateSourceCode("a.asm", testcode);
            code.DumpWords((str) => Console.WriteLine(str));

        }
        [TestMethod]
        public void TestNewLine()
        {
            const string testcode = @"
/*comment1*///comment 2
Main(/*param comment*/)//rightComment
{
  PUSH 1;PUSH 2
  ADD
  RET;
}
";

            var code = Neo.ASML.Parser.WordScanner.CreateSourceCode("a.asm", testcode);
            code.DumpWords((str) => Console.WriteLine(str));
            //check output

        }
        [TestMethod]
        public void TestLabel()
        {
            const string testcode = @"
/*comment1*///comment 2
Main()
{
  label1:
  label2:
  PUSH 1;PUSH 2
  88:99:ADD
  RET;
}
";

            var code = Neo.ASML.Parser.WordScanner.CreateSourceCode("a.asm", testcode);
            code.DumpWords((str) => Console.WriteLine(str));
            //check output

        }
        [TestMethod]
        public void TestStringAndArray()
        {
            const string testcode = @"
/*comment1*///comment 2
Main()
{
  PUSH ""hello""
  PUSH [2,0xa1,44]
  ADD
  RET
}
";

            var code = Neo.ASML.Parser.WordScanner.CreateSourceCode("a.asm", testcode);
            code.DumpWords((str) => Console.WriteLine(str));
            //check output

        }
    }
}
