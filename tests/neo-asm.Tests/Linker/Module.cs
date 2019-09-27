using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace neo_asm.Tests.Linker
{
    [TestClass]
    public class Module
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
            var module = Neo.ASML.Linker.Linker.CreateModule(proj);
            module.Dump((str) => Console.WriteLine(str));
            //check output
        }

        [TestMethod]
        public void TestValues()
        {
            const string testcode = @"
/*comment1*///comment 2
Main()
{
  PUSH ""aaa""
  PUSH 0x2244
  ADD
  RET
}
";

            var code = Neo.ASML.Parser.WordScanner.CreateSourceCode("a.asm", testcode);
            var proj = Neo.ASML.Parser.Parser.Parse(code);
            var module = Neo.ASML.Linker.Linker.CreateModule(proj);
            module.Dump((str) => Console.WriteLine(str));
            //check output
        }

        [TestMethod]
        public void TestJMP()
        {
            const string testcode = @"
/*comment1*///comment 2
Main()
{
  JMP helloha
  PUSH false;
helloha:
  PUSH ""aaa""
  PUSH 0x2244
  ADD
  RET
}
";

            var code = Neo.ASML.Parser.WordScanner.CreateSourceCode("a.asm", testcode);
            var proj = Neo.ASML.Parser.Parser.Parse(code);
            var module = Neo.ASML.Linker.Linker.CreateModule(proj);
            module.Dump((str) => Console.WriteLine(str));
            //check output
        }
    }
}
