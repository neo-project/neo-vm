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

        public VMState Execute()
        {
            if (engine.State == VMState.BREAK)
                engine.State = VMState.NONE;
            while (engine.State == VMState.NONE)
                ExecuteAndCheckBreakPoints();
            return engine.State;
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

        public VMState StepInto()
        {
            if (engine.State == VMState.HALT || engine.State == VMState.FAULT)
                return engine.State;
            engine.ExecuteNext();
            if (engine.State == VMState.NONE)
                engine.State = VMState.BREAK;
            return engine.State;
        }

        public VMState StepOut()
        {
            if (engine.State == VMState.BREAK)
                engine.State = VMState.NONE;
            int c = engine.InvocationStack.Count;
            while (engine.State == VMState.NONE && engine.InvocationStack.Count >= c)
                ExecuteAndCheckBreakPoints();
            if (engine.State == VMState.NONE)
                engine.State = VMState.BREAK;
            return engine.State;
        }

        public VMState StepOver()
        {
            if (engine.State == VMState.HALT || engine.State == VMState.FAULT)
                return engine.State;
            engine.State = VMState.NONE;
            int c = engine.InvocationStack.Count;
            do
            {
                ExecuteAndCheckBreakPoints();
            }
            while (engine.State == VMState.NONE && engine.InvocationStack.Count > c);
            if (engine.State == VMState.NONE)
                engine.State = VMState.BREAK;
            return engine.State;
        }
    }
}
