using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace neo_asm.Tests.execute
{
    [TestClass]
    public class Execute
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

            var engine = new Neo.VM.ExecutionEngine();
            engine.LoadScript(avm);
            var state = engine.Execute();

            Assert.IsTrue(state == Neo.VM.VMState.HALT);

            var result = engine.ResultStack.Peek().GetBigInteger();
            Assert.IsTrue(result == 3);
            //check output
        }

        [TestMethod]
        public void TestJMP()
        {
            const string testcode = @"
/*comment1*///comment 2
Main()
{
  PUSH 8
  JMP hello
hello:
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

            var engine = new Neo.VM.ExecutionEngine();
            engine.LoadScript(avm);
            var state = engine.Execute();

            Assert.IsTrue(state == Neo.VM.VMState.HALT);

            var result = engine.ResultStack.Peek().GetBigInteger();
            Assert.IsTrue(result == 3);
            //check output
        }
        [TestMethod]
        public void TestCALL()
        {
            const string testcode = @"
/*comment1*///comment 2
Main()
{
  PUSH 1
  PUSH 2
  CALL method01
  RET
}
method01()
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

            var engine = new Neo.VM.ExecutionEngine();
            engine.LoadScript(avm);
            var state = engine.Execute();

            Assert.IsTrue(state == Neo.VM.VMState.HALT);

            var result = engine.ResultStack.Peek().GetBigInteger();
            Assert.IsTrue(result == 3);
            //check output
        }
    }
}
