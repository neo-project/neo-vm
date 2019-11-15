using System;
using System.Diagnostics;
using System.Numerics;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Value={value}")]
    public class Boolean : PrimitiveType
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
            return base.Equals(other);
        }

        public override int GetByteLength()
        {
            return sizeof(bool);
        }

        public override BigInteger ToBigInteger()
        {
            return value ? BigInteger.One : BigInteger.Zero;
        }

        public override bool ToBoolean()
        {
            return value;
        }

        internal override ReadOnlyMemory<byte> ToMemory()
        {
            return value ? TRUE : FALSE;
        }
    }
}
