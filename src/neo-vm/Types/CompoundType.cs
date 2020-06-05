using System.Collections.Generic;
using System.Diagnostics;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Count={Count}")]
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

        public abstract void Clear();

        public override bool ToBoolean()
        {
            return true;
        }
    }
}
