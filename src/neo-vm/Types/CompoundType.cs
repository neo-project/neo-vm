using System;

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

        public abstract int ItemsCount { get; }

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
