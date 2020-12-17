using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types
{
    public abstract class PrimitiveType : StackItem
    {
        internal abstract ReadOnlyMemory<byte> Memory { get; }
        public virtual int Size => Memory.Length;

        public override StackItem ConvertTo(StackItemType type)
        {
            if (type == Type) return this;
            return type switch
            {
                StackItemType.Integer => GetInteger(),
                StackItemType.ByteString => Memory,
                StackItemType.Buffer => new Buffer(GetSpan()),
                _ => base.ConvertTo(type)
            };
        }

        internal sealed override StackItem DeepCopy(Dictionary<StackItem, StackItem> refMap)
        {
            return this;
        }

        public abstract override bool Equals(StackItem other);

        public abstract override int GetHashCode();

        public sealed override ReadOnlySpan<byte> GetSpan()
        {
            return Memory.Span;
        }

        public int ToInt32()
        {
            BigInteger i = GetInteger();
            if (i < int.MinValue || i > int.MaxValue) throw new InvalidCastException();
            return (int)i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(sbyte value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(byte value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(short value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(ushort value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(int value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(uint value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(long value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(ulong value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(BigInteger value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(bool value)
        {
            return (Boolean)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(byte[] value)
        {
            return (ByteString)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(ReadOnlyMemory<byte> value)
        {
            return (ByteString)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator PrimitiveType(string value)
        {
            return (ByteString)value;
        }
    }
}
