using Neo.VM.OpCodes;
using Neo.VM.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using VMArray = Neo.VM.Types.Array;

namespace Neo.VM
{
    public class ExecutionEngine : IDisposable
    {
        private readonly static JumpTable[] _jumpTable;

        /// <summary>
        /// Static contstructor
        /// </summary>
        static ExecutionEngine()
        {
            // Fill with all available opcodes

            _jumpTable = new JumpTable[255];

            foreach (var opcode in JumpTable.Init())
            {
                _jumpTable[(byte)opcode.OpCode] = opcode;
            }

            // Default Fill with errors

            Action<ExecutionEngine, ExecutionContext> error = (e, c) => e.State |= VMState.FAULT;

            for (int x = 0; x < 255; x++)
            {
                if (_jumpTable[x] == null)
                {
                    _jumpTable[x] = new JumpTable() { Execute = error };
                }
            }
        }

        private readonly IScriptTable table;
        private readonly Dictionary<byte[], HashSet<uint>> break_points = new Dictionary<byte[], HashSet<uint>>(new HashComparer());

        public IScriptContainer ScriptContainer { get; }
        public ICrypto Crypto { get; }
        public IInteropService Service { get; }
        public RandomAccessStack<ExecutionContext> InvocationStack { get; } = new RandomAccessStack<ExecutionContext>();
        public RandomAccessStack<StackItem> ResultStack { get; } = new RandomAccessStack<StackItem>();
        public ExecutionContext CurrentContext => InvocationStack.Peek();
        public ExecutionContext CallingContext => InvocationStack.Count > 1 ? InvocationStack.Peek(1) : null;
        public ExecutionContext EntryContext => InvocationStack.Peek(InvocationStack.Count - 1);
        public VMState State { get; protected set; } = VMState.BREAK;

        public ExecutionEngine(IScriptContainer container, ICrypto crypto, IScriptTable table = null, IInteropService service = null)
        {
            this.ScriptContainer = container;
            this.Crypto = crypto;
            this.table = table;
            this.Service = service;
        }

        public void AddBreakPoint(byte[] script_hash, uint position)
        {
            if (!break_points.TryGetValue(script_hash, out HashSet<uint> hashset))
            {
                hashset = new HashSet<uint>();
                break_points.Add(script_hash, hashset);
            }
            hashset.Add(position);
        }

        public virtual void Dispose()
        {
            while (InvocationStack.Count > 0)
                InvocationStack.Pop().Dispose();
        }

        public void Execute()
        {
            State &= ~VMState.BREAK;
            while (!State.HasFlag(VMState.HALT) && !State.HasFlag(VMState.FAULT) && !State.HasFlag(VMState.BREAK))
                StepInto();
        }

        public ExecutionContext LoadScript(byte[] script, int rvcount = -1)
        {
            ExecutionContext context = new ExecutionContext(this, script, rvcount);
            InvocationStack.Push(context);
            return context;
        }

        public bool RemoveBreakPoint(byte[] script_hash, uint position)
        {
            if (!break_points.TryGetValue(script_hash, out HashSet<uint> hashset))
                return false;
            if (!hashset.Remove(position))
                return false;
            if (hashset.Count == 0)
                break_points.Remove(script_hash);
            return true;
        }

        public void StepInto()
        {
            if (InvocationStack.Count == 0) State |= VMState.HALT;
            if (State.HasFlag(VMState.HALT) || State.HasFlag(VMState.FAULT)) return;

            var opcode = CurrentContext.InstructionPointer >= CurrentContext.Script.Length ? _jumpTable[0x66] : _jumpTable[CurrentContext.OpReader.ReadByte()];

            try
            {
                opcode.Execute(this, CurrentContext);
            }
            catch
            {
                State |= VMState.FAULT;
            }
        }

        public void StepOut()
        {
            State &= ~VMState.BREAK;
            int c = InvocationStack.Count;
            while (!State.HasFlag(VMState.HALT) && !State.HasFlag(VMState.FAULT) && !State.HasFlag(VMState.BREAK) && InvocationStack.Count >= c)
                StepInto();
        }

        public void StepOver()
        {
            if (State.HasFlag(VMState.HALT) || State.HasFlag(VMState.FAULT)) return;
            State &= ~VMState.BREAK;
            int c = InvocationStack.Count;
            do
            {
                StepInto();
            } while (!State.HasFlag(VMState.HALT) && !State.HasFlag(VMState.FAULT) && !State.HasFlag(VMState.BREAK) && InvocationStack.Count > c);
        }
    }
}