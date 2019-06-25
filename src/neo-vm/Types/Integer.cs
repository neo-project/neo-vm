﻿using System;
using System.Diagnostics;
using System.Numerics;

namespace Neo.VM.Types
{
    [DebuggerDisplay("type=Integer, value={value}")]
    public class Integer : StackItem
    {
        private static readonly byte[] ZeroBytes = new byte[0];

        private readonly BigInteger value;
        private int _length = -1;

        public Integer(BigInteger value)
        {
            this.value = value;
        }

        public override bool Equals(StackItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            if (other is Integer i) return value == i.value;
            byte[] bytes_other;
            try
            {
                bytes_other = other.GetByteArray();
            }
            catch (NotSupportedException)
            {
                return false;
            }
            return Unsafe.MemoryEquals(GetByteArray(), bytes_other);
        }

        public override BigInteger GetBigInteger() => value;

        public override bool GetBoolean() => !value.IsZero;

        public override byte[] GetByteArray()
        {
            return value.IsZero ? ZeroBytes : value.ToByteArray();
        }

        public override int GetByteLength()
        {
            if (_length == -1)
                _length = GetByteArray().Length;
            return _length;
        }
    }
}
