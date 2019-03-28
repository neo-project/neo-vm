using System;
using System.Numerics;

namespace Neo.VM.Types
{
    public class Boolean : StackItem
    {
        private static readonly ReadOnlyMemory<byte> TRUE = new byte[] { 1 };
        private static readonly ReadOnlyMemory<byte> FALSE = ReadOnlyMemory<byte>.Empty;

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
            ReadOnlyMemory<byte> bytes_other;
            try
            {
                bytes_other = other.GetByteArray();
            }
            catch (NotSupportedException)
            {
                return false;
            }
            return Unsafe.SpanEquals(GetByteArray().Span, bytes_other.Span);
        }

        public override BigInteger GetBigInteger()
        {
            return value ? BigInteger.One : BigInteger.Zero;
        }

        public override bool GetBoolean()
        {
            return value;
        }

        public override ReadOnlyMemory<byte> GetByteArray()
        {
            return value ? TRUE : FALSE;
        }
    }
}
