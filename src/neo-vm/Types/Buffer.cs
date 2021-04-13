using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Neo.VM.Types
{
    /// <summary>
    /// Represents a memory block that can be used for reading and writing in the VM.
    /// </summary>
    [DebuggerDisplay("Type={GetType().Name}, Value={System.BitConverter.ToString(InnerBuffer).Replace(\"-\", string.Empty)}")]
    public class Buffer : StackItem
    {
        /// <summary>
        /// The internal byte array used to store the actual data.
        /// </summary>
        public readonly byte[] InnerBuffer;

        /// <summary>
        /// The size of the buffer.
        /// </summary>
        public int Size => InnerBuffer.Length;
        public override StackItemType Type => StackItemType.Buffer;

        /// <summary>
        /// Create a buffer of the specified size.
        /// </summary>
        /// <param name="size">The size of this buffer.</param>
        public Buffer(int size)
        {
            InnerBuffer = new byte[size];
        }

        /// <summary>
        /// Create a buffer with the specified data.
        /// </summary>
        /// <param name="data">The data to be contained in this buffer.</param>
        public Buffer(ReadOnlySpan<byte> data)
            : this(data.Length)
        {
            if (!data.IsEmpty) data.CopyTo(InnerBuffer);
        }

        public override StackItem ConvertTo(StackItemType type)
        {
            switch (type)
            {
                case StackItemType.Integer:
                    if (InnerBuffer.Length > Integer.MaxSize)
                        throw new InvalidCastException();
                    return new BigInteger(InnerBuffer);
                case StackItemType.ByteString:
                    byte[] clone = new byte[InnerBuffer.Length];
                    InnerBuffer.CopyTo(clone.AsSpan());
                    return clone;
                default:
                    return base.ConvertTo(type);
            }
        }

        internal override StackItem DeepCopy(Dictionary<StackItem, StackItem> refMap)
        {
            if (refMap.TryGetValue(this, out StackItem? mappedItem)) return mappedItem;
            Buffer result = new(InnerBuffer);
            refMap.Add(this, result);
            return result;
        }

        public override bool GetBoolean()
        {
            return true;
        }

        public override ReadOnlySpan<byte> GetSpan()
        {
            return InnerBuffer;
        }
    }
}
