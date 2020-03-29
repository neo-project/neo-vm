using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Value={System.BitConverter.ToString(Memory.ToArray()).Replace(\"-\", string.Empty)}")]
    public class String : PrimitiveType
    {
        public static readonly String Empty = ReadOnlyMemory<byte>.Empty;

        internal override ReadOnlyMemory<byte> Memory { get; }
        public override StackItemType Type => StackItemType.String;

        public String(ReadOnlyMemory<byte> value)
        {
            this.Memory = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlyMemory<byte>(String value)
        {
            return value.Memory;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<byte>(String value)
        {
            return value.Memory.Span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator String(byte[] value)
        {
            return new String(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator String(ReadOnlyMemory<byte> value)
        {
            return new String(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator String(string value)
        {
            return new String(Encoding.UTF8.GetBytes(value));
        }
    }
}
