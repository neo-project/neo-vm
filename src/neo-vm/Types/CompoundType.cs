using System;
using System.Collections.Generic;

namespace Neo.VM.Types
{
    public abstract class CompoundType : StackItem
    {
        protected readonly ReferenceCounter ReferenceCounter;

        protected CompoundType(ReferenceCounter referenceCounter)
        {
            this.ReferenceCounter = referenceCounter;
        }

        public abstract int Count { get; }

        internal abstract IEnumerable<StackItem> SubItems { get; }

        internal abstract int SubItemsCount { get; }

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
    }
}
