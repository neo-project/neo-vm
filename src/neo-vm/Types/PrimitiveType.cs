using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types
{
    public abstract class PrimitiveType : StackItem
    {
        public override bool Equals(StackItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            if (!(other is PrimitiveType p)) return false;
            return Unsafe.MemoryEquals(ToByteArray(), p.ToByteArray());
        }

        public virtual int GetByteLength()
        {
            return ToByteArray().Length;
        }

        public sealed override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                foreach (byte element in ToByteArray())
                    hash = hash * 31 + element;
                return hash;
            }
        }

        public virtual BigInteger ToBigInteger()
        {
            if (GetByteLength() > ExecutionEngine.MaxSizeForBigInteger)
                throw new InvalidCastException();
            return new BigInteger(ToByteArray());
        }

        public override bool ToBoolean()
        {
            ReadOnlySpan<byte> value = ToByteArray();
            if (value.Length > ExecutionEngine.MaxSizeForBigInteger)
                return true;
            return Unsafe.NotZero(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> ToByteArray()
        {
            return ToMemory().Span;
        }

        internal abstract ReadOnlyMemory<byte> ToMemory();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(int value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(uint value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(long value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(ulong value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(BigInteger value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(bool value)
        {
            return (Boolean)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(byte[] value)
        {
            return (ByteArray)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(ReadOnlyMemory<byte> value)
        {
            return (ByteArray)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(string value)
        {
            return (ByteArray)value;
        }
    }
}
