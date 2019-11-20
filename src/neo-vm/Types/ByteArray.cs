using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Value={System.BitConverter.ToString(value.ToArray()).Replace(\"-\", string.Empty)}")]
    public class ByteArray : PrimitiveType
    {
        private readonly ReadOnlyMemory<byte> value;

        public ByteArray(ReadOnlyMemory<byte> value)
        {
            this.value = value;
        }

        public override int GetByteLength()
        {
            return value.Length;
        }

        internal override ReadOnlyMemory<byte> ToMemory()
        {
            return value;
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
