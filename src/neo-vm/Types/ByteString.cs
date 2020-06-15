using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Value={System.BitConverter.ToString(Memory.ToArray()).Replace(\"-\", string.Empty)}")]
    public class ByteString : PrimitiveType
    {
        public static readonly ByteString Empty = ReadOnlyMemory<byte>.Empty;

        internal override ReadOnlyMemory<byte> Memory { get; }
        public override StackItemType Type => StackItemType.ByteString;

        public ByteString(ReadOnlyMemory<byte> value)
        {
            this.Memory = value;
        }

        public override bool Equals(StackItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is ByteString b) return Span.SequenceEqual(b.Span);
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                foreach (byte element in Span)
                    hash = hash * 31 + element;
                return hash;
            }
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
            return new ByteString(Encoding.UTF8.GetBytes(value));
        }
    }
}
