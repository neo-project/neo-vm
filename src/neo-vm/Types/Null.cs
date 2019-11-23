namespace Neo.VM.Types
{
    public class Null : StackItem
    {
        internal Null() { }

        public override bool Equals(StackItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return true;
            if (other is Null) return true;
            return false;
        }

        public override bool ToBoolean()
        {
            return false;
        }
    }
}
