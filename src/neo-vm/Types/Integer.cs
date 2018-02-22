using System.Linq;
using System.Numerics;

namespace Neo.VM.Types
{
    public class Integer : StackItem
    {
        private BigInteger value;

        public Integer(BigInteger value)
        {
            this.value = value;
        }

        public override bool Equals(StackItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            Integer i = other as Integer;
            if (i == null)
                return GetByteArray().SequenceEqual(other.GetByteArray());
            else
                return value == i.value;
        }

        public override BigInteger GetBigInteger()
        {
            return value;
        }

        public override bool GetBoolean()
        {
            return value != BigInteger.Zero;
        }

        public override byte[] GetByteArray()
        {
            return value.ToByteArray();
        }
    }
}
