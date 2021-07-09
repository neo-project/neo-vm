using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types
{
    /// <summary>
    /// Represents an immutable memory block in the VM.
    /// </summary>
    [DebuggerDisplay("Type={GetType().Name}, Value={System.BitConverter.ToString(Memory.ToArray()).Replace(\"-\", string.Empty)}")]
    public class ByteString : PrimitiveType
    {
        /// <summary>
        /// The largest comparable size. If a <see cref="ByteString"/> exceeds this size, comparison operations on it cannot be performed in the VM.
        /// </summary>
        public const int MaxComparableSize = ushort.MaxValue;

        private int _hashCode = -1;

        /// <summary>
        /// An empty <see cref="ByteString"/>.
        /// </summary>
        public static readonly ByteString Empty = ReadOnlyMemory<byte>.Empty;

        internal override ReadOnlyMemory<byte> Memory { get; }
        public override StackItemType Type => StackItemType.ByteString;

        /// <summary>
        /// Create a new <see cref="ByteString"/> with the specified data.
        /// </summary>
        /// <param name="data">The data to be contained in this <see cref="ByteString"/>.</param>
        public ByteString(ReadOnlyMemory<byte> data)
        {
            this.Memory = data;
        }

        public override bool Equals(StackItem? other)
        {
            if (Size > MaxComparableSize)
                throw new InvalidOperationException("The operand exceeds the maximum comparable size.");
            if (ReferenceEquals(this, other)) return true;
            if (other is not ByteString b) return false;
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
            if (_hashCode != -1) return _hashCode;
            unchecked
            {
                _hashCode = 17;
                var buffer = GetSpan();
                for (int x = 0, max = buffer.Length; x < max;)
                {
                    var left = max - x;
                    if (left >= 8)
                    {
                        _hashCode = HashCode.Combine(_hashCode, BinaryPrimitives.ReadInt64LittleEndian(buffer.Slice(x)));
                        x += 8;
                    }
                    else if (left >= 4)
                    {
                        _hashCode = HashCode.Combine(_hashCode, BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(x)));
                        x += 4;
                    }
                    else if (left >= 2)
                    {
                        _hashCode = HashCode.Combine(_hashCode, BinaryPrimitives.ReadInt16LittleEndian(buffer.Slice(x)));
                        x += 2;
                    }
                    else
                    {
                        _hashCode = HashCode.Combine(_hashCode, buffer[x]);
                        x++;
                    }
                }
                return _hashCode;
            }
        }

        public override BigInteger GetInteger()
        {
            if (Size > Integer.MaxSize) throw new InvalidCastException($"MaxSize exceed: {Size}");
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
