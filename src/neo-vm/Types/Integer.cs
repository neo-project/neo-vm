using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Value={value}")]
    public class Integer : PrimitiveType
    {
        public const int MaxSize = 32;

        private readonly int _length;
        private readonly BigInteger value;

        public Integer(BigInteger value)
        {
            if (value.IsZero)
            {
                _length = 0;
            }
            else
            {
                _length = value.GetByteCount();
                if (_length > MaxSize) throw new ArgumentException();
            }
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
