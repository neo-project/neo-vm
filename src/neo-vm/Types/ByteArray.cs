using System;
using System.Diagnostics;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Value={System.BitConverter.ToString(value).Replace(\"-\", string.Empty)}")]
    public class ByteArray : StackItem
    {
        private readonly ReadOnlyMemory<byte> value;

        public ByteArray(ReadOnlyMemory<byte> value)
        {
            this.value = value;
        }

        public override bool Equals(StackItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            ReadOnlyMemory<byte> bytes_other;
            try
            {
                bytes_other = other.GetByteArray();
            }
            catch (NotSupportedException)
            {
                return false;
            }
            return Unsafe.MemoryEquals(value.Span, bytes_other.Span);
        }

        public override bool GetBoolean()
        {
            if (value.Length > ExecutionEngine.MaxSizeForBigInteger)
                return true;
            return Unsafe.NotZero(value.Span);
        }

        public override ReadOnlyMemory<byte> GetByteArray()
        {
            return value;
        }
    }
}
