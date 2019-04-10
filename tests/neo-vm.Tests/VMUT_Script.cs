using System;
using System.Linq;
using System.Numerics;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Test.Helpers;
using Neo.VM;
using Neo.Test.Types;

namespace Neo.Test
{
    [TestClass]
    public class VMUT_Script
    {
        Script uut;
        
        [TestInitialize]
        public void TestSetup()
        {
            uut = new Script(new Crypto(), new byte[1]{0x00});
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }

        [TestMethod]
        public void TestScriptHash()
        {
            byte[] b = new byte[]{0x9F, 0x7F, 0xD0, 0x96, 0xD3, 0x7E, 0xD2, 0xC0, 0xE3, 0xF7, 0xF0, 0xCF, 0xC9, 0x24, 0xBE, 0xEF, 0x4F, 0xFC, 0xEB, 0x68};
            uut.ScriptHash.Should().BeEquivalentTo(b);
        }
    }
}
