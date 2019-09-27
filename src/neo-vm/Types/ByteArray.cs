using System;
using System.Diagnostics;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Value={System.BitConverter.ToString(value).Replace(\"-\", string.Empty)}")]
    public class ByteArray : StackItem
    {
        private readonly byte[] value;

        public override bool IsNull => (value is null );

        public ByteArray(byte[] value)
        {
            this.value = value;
        }

        public override bool Equals(StackItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            byte[] bytes_other;
            try
            {
                bytes_other = other.GetByteArray();
            }
            catch (NotSupportedException)
            {
                return false;
            }
            return Unsafe.MemoryEquals(value, bytes_other);
        }

        public override bool GetBoolean()
        {
            if (value.Length > ExecutionEngine.MaxSizeForBigInteger)
                return true;
            return Unsafe.NotZero(value);
        }

        public override byte[] GetByteArray()
        {
            // implicit conversion to byte[0], if null
            return (value is null) ? new byte[0] : value;
        }
    }
}
