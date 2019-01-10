using Neo.VM.Types;
using Xunit;

namespace Neo.Test
{
    public class StructTest
    {
        private readonly Struct @struct;

        public StructTest()
        {
            @struct = new Struct { 1 };
            for (int i = 0; i < 20000; i++)
                @struct = new Struct { @struct };
        }

        [Fact]
        public void Clone()
        {
            Struct s1 = new Struct { 1, new Struct { 2 } };
            Struct s2 = s1.Clone();
            s1[0] = 3;
            Assert.Equal(1, s2[0]);
            ((Struct)s1[1])[0] = 3;
            Assert.Equal(2, ((Struct)s2[1])[0]);
            @struct.Clone();
        }

        [Fact]
#pragma warning disable xUnit1024 // Test methods cannot have overloads
        public void Equals()
#pragma warning restore xUnit1024 // Test methods cannot have overloads
        {
            Struct s1 = new Struct { 1, new Struct { 2 } };
            Struct s2 = new Struct { 1, new Struct { 2 } };
            Assert.True(s1.Equals(s2));
            Struct s3 = new Struct { 1, new Struct { 3 } };
            Assert.False(s1.Equals(s3));
            Assert.True(@struct.Equals(@struct.Clone()));
        }
    }
}
