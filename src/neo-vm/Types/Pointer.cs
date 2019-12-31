using System.Diagnostics;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Position={Position}")]
    public class Pointer : StackItem
    {
        public Script Script { get; }
        public int Position { get; }
        public override StackItemType Type => StackItemType.Pointer;

        public Pointer(Script script, int position)
        {
            this.Script = script;
            this.Position = position;
        }

        public override bool Equals(object obj)
        {
            if (obj == this) return true;
            if (obj is Pointer p) return Position == p.Position && Script.Equals(p.Script);
            return false;
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode() + (31 * Script.GetHashCode());
        }

        public override bool ToBoolean()
        {
            return true;
        }
    }
}
