using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Value={value}")]
    public class Integer : PrimitiveType
    {
        private int _length = -1;
        private readonly BigInteger value;

        public Integer(BigInteger value)
        {
            this.value = value;
        }

        public override bool Equals(StackItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            if (other is Integer i) return value == i.value;
            return base.Equals(other);
        }

        public override int GetByteLength()
        {
            if (_length == -1)
                _length = value.GetByteCount();
            return _length;
        }

        public override BigInteger ToBigInteger()
        {
            return value;
        }

        public override bool ToBoolean()
        {
            return !value.IsZero;
        }

        internal override ReadOnlyMemory<byte> ToMemory()
        {
            return value.IsZero ? ReadOnlyMemory<byte>.Empty : value.ToByteArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Integer(int value)
        {
            return (BigInteger)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Integer(uint value)
        {
            return (BigInteger)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Integer(long value)
        {
            return (BigInteger)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Integer(ulong value)
        {
            return (BigInteger)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Integer(BigInteger value)
        {
            return new Integer(value);
        }
    }
}
