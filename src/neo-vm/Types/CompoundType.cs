using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types
{
    public abstract class CompoundType : StackItem
    {
        public abstract int Count { get; }

        public abstract void Clear();

        public override bool Equals(StackItem other)
        {
            return ReferenceEquals(this, other);
        }

        public override int GetHashCode()
        {
            throw new NotSupportedException();
        }

        public override bool ToBoolean()
        {
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator CompoundType(StackItem[] value)
        {
            return (Array)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator CompoundType(List<StackItem> value)
        {
            return (Array)value;
        }
    }
}
