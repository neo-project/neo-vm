using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Value={System.BitConverter.ToString(memory.ToArray()).Replace(\"-\", string.Empty)}")]
    public class ByteArray : PrimitiveType
    {
        private readonly ReadOnlyMemory<byte> memory;

        public override int Size => memory.Length;
        public override ReadOnlySpan<byte> Span => memory.Span;

        public ByteArray(ReadOnlyMemory<byte> value)
        {
            this.memory = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlyMemory<byte>(ByteArray value)
        {
            return value.memory;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<byte>(ByteArray value)
        {
            return value.memory.Span;
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
