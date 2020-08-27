using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Value={System.BitConverter.ToString(Memory.ToArray()).Replace(\"-\", string.Empty)}")]
    public class ByteString : PrimitiveType
    {
        public const int MaxComparableSize = ushort.MaxValue;

        public static readonly ByteString Empty = ReadOnlyMemory<byte>.Empty;

        internal override ReadOnlyMemory<byte> Memory { get; }
        public override StackItemType Type => StackItemType.ByteString;

        public ByteString(ReadOnlyMemory<byte> value)
        {
            this.Memory = value;
        }

        public override bool Equals(StackItem other)
        {
            if (Size > MaxComparableSize)
                throw new InvalidOperationException("The operand exceeds the maximum comparable size.");
            if (ReferenceEquals(this, other)) return true;
            if (!(other is ByteString b)) return false;
            if (b.Size > MaxComparableSize)
                throw new InvalidOperationException("The operand exceeds the maximum comparable size.");
            return GetSpan().SequenceEqual(b.GetSpan());
        }

        public override bool GetBoolean()
        {
            if (Size > Integer.MaxSize) throw new InvalidCastException();
            return Unsafe.NotZero(GetSpan());
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                foreach (byte element in GetSpan())
                    hash = hash * 31 + element;
                return hash;
            }
        }

        public override BigInteger GetInteger()
        {
            if (Size > Integer.MaxSize) throw new InvalidCastException();
            return new BigInteger(GetSpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlyMemory<byte>(ByteString value)
        {
            return value.Memory;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<byte>(ByteString value)
        {
            return value.Memory.Span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ByteString(byte[] value)
        {
            return new ByteString(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ByteString(ReadOnlyMemory<byte> value)
        {
            return new ByteString(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ByteString(string value)
        {
            return new ByteString(Utility.StrictUTF8.GetBytes(value));
        }
    }
}
