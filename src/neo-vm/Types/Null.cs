using System;

namespace Neo.VM.Types
{
    public class Null : StackItem
    {
        internal Null() { }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is null) return true;
            if (obj is Null) return true;
            return false;
        }

        public override int GetHashCode()
        {
            throw new NotSupportedException();
        }

        public override bool ToBoolean()
        {
            return false;
        }
    }
}
