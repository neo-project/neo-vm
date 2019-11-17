using System;
using System.Diagnostics;
using System.Numerics;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Value={value}")]
    public class Boolean : StackItem
    {
        private static readonly byte[] TRUE = new byte[] { 1 };
        private static readonly byte[] FALSE = new byte[] { 0 };

        private readonly bool value;

        public Boolean(bool value)
        {
            this.value = value;
        }

        public override bool Equals(StackItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            if (other is Boolean b) return value == b.value;
            ReadOnlySpan<byte> bytes_other;
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
            return value ? BigInteger.One : BigInteger.Zero;
        }

        public override bool GetBoolean()
        {
            return value;
        }

        public override ReadOnlySpan<byte> GetByteArray()
        {
            return value ? TRUE : FALSE;
        }

        internal override ReadOnlyMemory<byte> ToMemory()
        {
            return value ? TRUE : FALSE;
        }
    }
}
