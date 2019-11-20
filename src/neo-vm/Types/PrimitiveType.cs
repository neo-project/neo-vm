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
    }
}
