using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Value={System.BitConverter.ToString(Memory.ToArray()).Replace(\"-\", string.Empty)}")]
    public class ByteArray : PrimitiveType
    {
        public static readonly ByteArray Empty = ReadOnlyMemory<byte>.Empty;

        internal override ReadOnlyMemory<byte> Memory { get; }
        public override StackItemType Type => StackItemType.ByteArray;

        public ByteArray(ReadOnlyMemory<byte> value)
        {
            this.Memory = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlyMemory<byte>(ByteArray value)
        {
            return value.Memory;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<byte>(ByteArray value)
        {
            return value.Memory.Span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ByteArray(byte[] value)
        {
            return new ByteArray(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ByteArray(ReadOnlyMemory<byte> value)
        {
            return new ByteArray(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ByteArray(string value)
        {
            return new ByteArray(Encoding.UTF8.GetBytes(value));
        }
    }
}
