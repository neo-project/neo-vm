using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM.Types;

namespace Neo.Test
{
    [TestClass]
    public class UtStruct
    {
        private readonly Struct @struct;

        public UtStruct()
        {
            @struct = new Struct { 1 };
            for (int i = 0; i < 20000; i++)
                @struct = new Struct { @struct };
        }

        [TestMethod]
#pragma warning disable xUnit1024 // Test methods cannot have overloads
        public void Equals()
#pragma warning restore xUnit1024 // Test methods cannot have overloads
        {
            Struct s1 = new Struct { 1, new Struct { 2 } };
            Struct s2 = new Struct { 1, new Struct { 2 } };
            Assert.IsTrue(s1.Equals(s2));
            Struct s3 = new Struct { 1, new Struct { 3 } };
            Assert.IsFalse(s1.Equals(s3));
        }
    }
}
