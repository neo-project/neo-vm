using System.Diagnostics;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Position={Position}")]
    public class Pointer : StackItem
    {
        public int Position { get; }

        public Pointer(int position)
        {
            this.Position = position;
        }

        public override bool Equals(object obj)
        {
            if (obj == this) return true;
            if (obj is Pointer p) return Position == p.Position;
            return false;
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }

        public override bool ToBoolean()
        {
            return true;
        }
    }
}
