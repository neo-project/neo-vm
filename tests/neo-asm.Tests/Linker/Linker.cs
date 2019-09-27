using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace neo_asm.Tests.Linker
{
    [TestClass]
    public class Linker
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
            var avm = Neo.ASML.Linker.Linker.Link(module, "Main");

            module.Dump((str) => Console.WriteLine(str));
            Console.WriteLine("avm=" + Helper.Hex2Str(avm));
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
            var avm = Neo.ASML.Linker.Linker.Link(module, "Main");

            module.Dump((str) => Console.WriteLine(str));
            Console.WriteLine("avm=" + Helper.Hex2Str(avm));
            //check output
        }
        [TestMethod]
        public void TestCall()
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
            var module = Neo.ASML.Linker.Linker.CreateModule(proj);
            var avm = Neo.ASML.Linker.Linker.Link(module, "Main");

            module.Dump((str) => Console.WriteLine(str));
            Console.WriteLine("avm=" + Helper.Hex2Str(avm));
            //check output
        }
    }
}
