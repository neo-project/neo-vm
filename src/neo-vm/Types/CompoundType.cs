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

        public override bool ToBoolean()
        {
            return true;
        }
    }
}
