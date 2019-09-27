using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace neo_asm.Tests.dasm
{
    [TestClass]
    public class DASM
    {
        [TestMethod]
        public void TestBasecode()
        {
            const string testcode = "51529366";
            var data = Helper.Str2Hex(testcode);
            var proj = Neo.ASML.DASM.Parse(data);
            var srccode = Neo.ASML.DASM.GenSource(proj);
            Console.WriteLine("srccode=" + srccode);
            //check output
        }
        [TestMethod]
        public void TestJMP()
        {
            const string testcode = "62040000032261610244229366";
            var data = Helper.Str2Hex(testcode);
            var proj = Neo.ASML.DASM.Parse(data);
            var srccode = Neo.ASML.DASM.GenSource(proj);
            Console.WriteLine("srccode=" + srccode);
            //check output
        }
        [TestMethod]
        public void TestCALL()
        {
            const string testcode = "5152650400669366";
            var data = Helper.Str2Hex(testcode);
            var proj = Neo.ASML.DASM.Parse(data);
            var srccode = Neo.ASML.DASM.GenSource(proj);
            Console.WriteLine("srccode=" + srccode);
            //check output
        }
        [TestMethod]
        public void TestJMPandCALL()
        {
            const string testcode = "515265040066620C0002B8220522616161619366";
            var data = Helper.Str2Hex(testcode);
            var proj = Neo.ASML.DASM.Parse(data);
            var srccode = Neo.ASML.DASM.GenSource(proj);
            Console.WriteLine("srccode=" + srccode);
            //check output
        }
    }
}
