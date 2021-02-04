using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types
{
    /// <summary>
    /// Represents a boolean (<see langword="true" /> or <see langword="false" />) value in the VM.
    /// </summary>
    [DebuggerDisplay("Type={GetType().Name}, Value={value}")]
    public class Boolean : PrimitiveType
    {
        private static readonly ReadOnlyMemory<byte> TRUE = new byte[] { 1 };
        private static readonly ReadOnlyMemory<byte> FALSE = new byte[] { 0 };

        private readonly bool value;

        internal override ReadOnlyMemory<byte> Memory => value ? TRUE : FALSE;
        public override int Size => sizeof(bool);
        public override StackItemType Type => StackItemType.Boolean;

        /// <summary>
        /// Create a new VM object representing the boolean type.
        /// </summary>
        /// <param name="value">The initial value of the object.</param>
        public Boolean(bool value)
        {
            this.value = value;
        }

        public override bool Equals(StackItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is Boolean b) return value == b.value;
            return false;
        }

        public override bool GetBoolean()
        {
            return value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(value);
        }

        public override BigInteger GetInteger()
        {
            return value ? BigInteger.One : BigInteger.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Boolean(bool value)
        {
            return new Boolean(value);
        }
    }
}
