using System.Collections.Generic;
using System.Diagnostics;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Count={Count}")]
    public class Struct : Array
    {
        public Struct(ReservedMemory memory) : base(memory) { }

        public Struct(ReservedMemory memory, IEnumerable<StackItem> value) : base(memory, value) { }

        public Struct Clone(ReservedMemory memory)
        {
            Struct @struct = new Struct(memory);
            Queue<Struct> queue = new Queue<Struct>();
            queue.Enqueue(@struct);
            queue.Enqueue(this);
            while (queue.Count > 0)
            {
                Struct a = queue.Dequeue();
                Struct b = queue.Dequeue();
                foreach (StackItem item in b)
                {
                    if (item is Struct sb)
                    {
                        Struct sa = new Struct(memory);
                        a.Add(sa);
                        queue.Enqueue(sa);
                        queue.Enqueue(sb);
                    }
                    else
                    {
                        a.Add(item);
                    }
                }
            }
            return @struct;
        }

        public override bool Equals(StackItem other)
        {
            if (other is null) return false;
            Stack<StackItem> stack1 = new Stack<StackItem>();
            Stack<StackItem> stack2 = new Stack<StackItem>();
            stack1.Push(this);
            stack2.Push(other);
            while (stack1.Count > 0)
            {
                StackItem a = stack1.Pop();
                StackItem b = stack2.Pop();
                if (a is Struct sa)
                {
                    if (ReferenceEquals(a, b)) continue;
                    if (!(b is Struct sb)) return false;
                    if (sa.Count != sb.Count) return false;
                    foreach (StackItem item in sa)
                        stack1.Push(item);
                    foreach (StackItem item in sb)
                        stack2.Push(item);
                }
                else
                {
                    if (!a.Equals(b)) return false;
                }
            }
            return true;
        }
    }
}
