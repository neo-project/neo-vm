using System;
using System.Linq;
using System.Numerics;

namespace Neo.VM.Types
{
    public class Boolean : StackItem
    {
        private static readonly byte[] TRUE = { 1 };
        private static readonly byte[] FALSE = new byte[0];

        private bool value;

        public Boolean(bool value)
        {
            this.value = value;
        }

        public override bool Equals(StackItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            if (other is Boolean b) return value == b.value;
            byte[] bytes_other;
            try
            {
                bytes_other = other.GetByteArray();
            }
            catch (NotSupportedException)
            {
                return false;
            }
            return GetByteArray().SequenceEqual(bytes_other);
        }

        public override BigInteger GetBigInteger()
        {
            return value ? BigInteger.One : BigInteger.Zero;
        }

        public override bool GetBoolean()
        {
            return value;
        }

        public override byte[] GetByteArray()
        {
            return value ? TRUE : FALSE;
        }
    }
}
