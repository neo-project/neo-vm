using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM.Types;

namespace Neo.Test
{
    [TestClass]
    public class UtStruct
    {
        private readonly Struct s;

        public UtStruct()
        {
            s = new Struct { 1 };
            for (int i = 0; i < 20000; i++)
                s = new Struct { s };
        }

        [TestMethod]
        public void Clone()
        {
            Struct s1 = new Struct { 1, new Struct { 2 } };
            Struct s2 = s1.Clone();
            s1[0] = 3;
            Assert.AreEqual(1, s2[0]);
            ((Struct)s1[1])[0] = 3;
            Assert.AreEqual(2, ((Struct)s2[1])[0]);
            s.Clone();
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
            Assert.IsTrue(s.Equals(s.Clone()));
        }
    }
}
