using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace neo_asm.Tests.Parser
{
    [TestClass]
    public class ProjParser
    {
        [TestMethod]
        public void TestBaseCode()
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
            var proj = Neo.ASML.Parser.Parser.Parse(code);
            proj.Dump((str) => Console.WriteLine(str));
            //check output

        }
        [TestMethod]
        public void TestMultiFunction()
        {
            const string testcode = @"
/*comment1*///comment 2
Main()
{
  PUSH 1
  PUSH 2
  CALL method1
  RET
}
method1()
{
  ADD
  RET
}
";

            var code = Neo.ASML.Parser.WordScanner.CreateSourceCode("a.asm", testcode);
            var proj = Neo.ASML.Parser.Parser.Parse(code);
            proj.Dump((str) => Console.WriteLine(str));
            //check output

        }
        [TestMethod]
        public void TestComment()
        {
            const string testcode = @"
/*comment1*///comment 2
Main(/*param comment*/)//rightComment
{
  PUSH 1
  PUSH 2
  CALL method1 //call func
  RET
}

//this is a new method
method1()
{
label://no use 
  ADD
  RET
}
";

            var code = Neo.ASML.Parser.WordScanner.CreateSourceCode("a.asm", testcode);
            var proj = Neo.ASML.Parser.Parser.Parse(code);
            proj.Dump((str) => Console.WriteLine(str));
            //check output

        }
        [TestMethod]
        public void TestValues()
        {
            const string testcode = @"
/*comment1*///comment 2
Main(/*param comment*/)//rightComment
{
  PUSH 1
  PUSH 0x02
  PUSH ""abc"";
  PUSH true
  PUSH [1,2,3];
  CALL method1 //call func
  RET
}

//this is a new method
method1()
{
label://no use 
  ADD
  RET
}
";

            var code = Neo.ASML.Parser.WordScanner.CreateSourceCode("a.asm", testcode);
            var proj = Neo.ASML.Parser.Parser.Parse(code);
            proj.Dump((str) => Console.WriteLine(str));
            //check output

        }
    }
}
