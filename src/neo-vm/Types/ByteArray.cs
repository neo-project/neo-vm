using System;
using System.Diagnostics;
using System.Text;

namespace Neo.VM.Types
{
    [DebuggerDisplay("type=ByteArray, value={HexValue}")]
    public class ByteArray : StackItem
    {
        private byte[] value;

        /// <summary>
        /// Return Hexadecimal value
        /// </summary>
        public string HexValue
        {
            get
            {
                var hex = new StringBuilder(value.Length * 2);
                foreach (var b in value) hex.AppendFormat("{0:X2}", b);
                return hex.ToString();
            }
        }

        public ByteArray(byte[] value)
        {
            this.value = value;
        }

        public override bool Equals(StackItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
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
            return value;
        }
    }
}
