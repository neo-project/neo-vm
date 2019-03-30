using System.Collections.Generic;

namespace Neo.VM
{
    public class DebugeableExecutionEngine : ExecutionEngine
    {
        private readonly Dictionary<byte[], HashSet<uint>> break_points = new Dictionary<byte[], HashSet<uint>>(new HashComparer());

        public DebugeableExecutionEngine(IScriptContainer container, ICrypto crypto, IScriptTable table = null, IInteropService service = null) :
            base(container, crypto, table, service)
        { }

        public void AddBreakPoint(byte[] script_hash, uint position)
        {
            if (!break_points.TryGetValue(script_hash, out HashSet<uint> hashset))
            {
                hashset = new HashSet<uint>();
                break_points.Add(script_hash, hashset);
            }
            hashset.Add(position);
        }

        protected override bool PostExecuteInstruction(Instruction instruction)
        {
            var ret = base.PostExecuteInstruction(instruction);

            if (State == VMState.NONE && InvocationStack.Count > 0)
            {
                if (break_points.Count > 0 && break_points.TryGetValue(CurrentContext.ScriptHash, out HashSet<uint> hashset) && hashset.Contains((uint)CurrentContext.InstructionPointer))
                    State = VMState.BREAK;
            }

            return ret;
        }

        public bool RemoveBreakPoint(byte[] script_hash, uint position)
        {
            if (!break_points.TryGetValue(script_hash, out HashSet<uint> hashset)) return false;
            if (!hashset.Remove(position)) return false;
            if (hashset.Count == 0) break_points.Remove(script_hash);

            return true;
        }
    }
}