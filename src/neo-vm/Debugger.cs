using System.Collections.Generic;

namespace Neo.VM
{
    public class Debugger
    {
        private readonly ExecutionEngine engine;
        private readonly Dictionary<Script, HashSet<uint>> break_points = new Dictionary<Script, HashSet<uint>>();

        public Debugger(ExecutionEngine engine)
        {
            this.engine = engine;
        }

        public void AddBreakPoint(Script script, uint position)
        {
            if (!break_points.TryGetValue(script, out HashSet<uint> hashset))
            {
                hashset = new HashSet<uint>();
                break_points.Add(script, hashset);
            }
            hashset.Add(position);
        }

        public void Execute()
        {
            engine.State &= ~VMState.BREAK;
            while (!engine.State.HasFlag(VMState.HALT) && !engine.State.HasFlag(VMState.FAULT) && !engine.State.HasFlag(VMState.BREAK))
            {
                ExecuteAndCheckBreakPoints();
            }
        }

        private void ExecuteAndCheckBreakPoints()
        {
            engine.ExecuteNext();
            if (engine.State == VMState.NONE && engine.InvocationStack.Count > 0 && break_points.Count > 0)
            {
                if (break_points.TryGetValue(engine.CurrentContext.Script, out HashSet<uint> hashset) && hashset.Contains((uint)engine.CurrentContext.InstructionPointer))
                    engine.State = VMState.BREAK;
            }
        }

        public bool RemoveBreakPoint(Script script, uint position)
        {
            if (!break_points.TryGetValue(script, out HashSet<uint> hashset)) return false;
            if (!hashset.Remove(position)) return false;
            if (hashset.Count == 0) break_points.Remove(script);
            return true;
        }

        public void StepInto()
        {
            if (engine.State.HasFlag(VMState.HALT) || engine.State.HasFlag(VMState.FAULT)) return;
            engine.ExecuteNext();
            if (engine.State == VMState.NONE)
                engine.State = VMState.BREAK;
        }

        public void StepOut()
        {
            engine.State &= ~VMState.BREAK;
            int c = engine.InvocationStack.Count;
            while (!engine.State.HasFlag(VMState.HALT) && !engine.State.HasFlag(VMState.FAULT) && !engine.State.HasFlag(VMState.BREAK) && engine.InvocationStack.Count >= c)
                ExecuteAndCheckBreakPoints();
            if (engine.State == VMState.NONE)
                engine.State = VMState.BREAK;
        }

        public void StepOver()
        {
            if (engine.State.HasFlag(VMState.HALT) || engine.State.HasFlag(VMState.FAULT)) return;
            engine.State &= ~VMState.BREAK;
            int c = engine.InvocationStack.Count;
            do
            {
                ExecuteAndCheckBreakPoints();
            }
            while (!engine.State.HasFlag(VMState.HALT) && !engine.State.HasFlag(VMState.FAULT) && !engine.State.HasFlag(VMState.BREAK) && engine.InvocationStack.Count > c);
            if (engine.State == VMState.NONE)
                engine.State = VMState.BREAK;
        }
    }
}
