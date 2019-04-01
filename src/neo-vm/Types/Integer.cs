using System;
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
            if (other is Integer i) return value == i.value;
            byte[] bytes_other;
            try
            {
                bytes_other = other.GetByteArray();
            }
            catch (NotSupportedException)
            {
                return false;
            }
            return Unsafe.MemoryEquals(GetByteArray(), bytes_other);
        }

        public override BigInteger GetBigInteger()
        {
            return value;
        }

        public override bool GetBoolean()
        {
            return !value.IsZero;
        }

        public override byte[] GetByteArray()
        {
            return value.ToByteArray();
        }

        private int _length = -1;
        public override int GetByteLength()
        {
            if (_length == -1)
                _length = value.ToByteArray().Length;
            return _length;
        }
    }
}
