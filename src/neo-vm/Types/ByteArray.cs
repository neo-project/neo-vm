using System;

namespace Neo.VM.Types
{
    public class ByteArray : StackItem
    {
        private ReadOnlyMemory<byte> value;

        public ByteArray(ReadOnlyMemory<byte> value)
        {
            this.value = value;
        }

        public override bool Equals(StackItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            ReadOnlyMemory<byte> bytes_other;
            try
            {
                bytes_other = other.GetByteArray();
            }
            catch (NotSupportedException)
            {
                return false;
            }
            return Unsafe.SpanEquals(value.Span, bytes_other.Span);
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
