using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Neo.VM.Types
{
    /// <summary>
    /// The base class for complex types in the VM.
    /// </summary>
    [DebuggerDisplay("Type={GetType().Name}, Count={Count}")]
    public abstract class CompoundType : StackItem
    {
        /// <summary>
        /// The reference counter used to count the items in the VM object.
        /// </summary>
        protected readonly ReferenceCounter ReferenceCounter;

        /// <summary>
        /// Create a new <see cref="CompoundType"/> with the specified reference counter.
        /// </summary>
        /// <param name="referenceCounter">The reference counter to be used.</param>
        protected CompoundType(ReferenceCounter referenceCounter)
        {
            this.ReferenceCounter = referenceCounter;
            referenceCounter?.AddZeroReferred(this);
        }

        /// <summary>
        /// The number of items in this VM object.
        /// </summary>
        public abstract int Count { get; }

        internal abstract IEnumerable<StackItem> SubItems { get; }

        internal abstract int SubItemsCount { get; }

        /// <summary>
        /// Remove all items from the VM object.
        /// </summary>
        public abstract void Clear();

        internal abstract override StackItem DeepCopy(Dictionary<StackItem, StackItem> refMap);

        public sealed override bool GetBoolean()
        {
            return true;
        }

        /// <summary>
        /// The operation is not supported. Always throw <see cref="NotSupportedException"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">This method always throws the exception.</exception>
        public override int GetHashCode()
        {
            throw new NotSupportedException();
        }
    }
}
