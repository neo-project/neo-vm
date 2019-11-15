using System;
using System.Diagnostics;

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
    }
}
