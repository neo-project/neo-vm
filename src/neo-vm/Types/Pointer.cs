using System;
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

        public override bool Equals(StackItem other)
        {
            if (other == this) return true;
            if (other is Pointer p) return Position == p.Position && Script == p.Script;
            return false;
        }

        public override bool GetBoolean()
        {
            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Script, Position);
        }
    }
}
