namespace Neo.VM
{
    public class InstructionSet
    {
        /// <summary>
        /// Default instruction set
        /// </summary>
        public static readonly InstructionSet Default = new InstructionSet();

        private static readonly byte[] EmptyBytes = new byte[0];
        private delOnInstruction[] _jumpTable = null;

        public delegate bool delOnInstruction(ExecutionEngine engine, ExecutionContext context, Instruction instruction);

        /// <summary>
        /// Get default jump table
        /// </summary>
        /// <returns>Return instruction array</returns>
        public virtual delOnInstruction[] GetJumpTable()
        {
            if (_jumpTable != null) return _jumpTable;

            _jumpTable = new delOnInstruction[byte.MaxValue];

            for (byte x = (byte)OpCode.PUSHBYTES1; x <= (byte)OpCode.PUSHDATA4; x++)
            {
                _jumpTable[x] = PUSHBYTES1_PUSHBYTES75;
            }

            _jumpTable[(byte)OpCode.PUSH0] = PUSH0;
            _jumpTable[(byte)OpCode.PUSHM1] = PUSHM1_PUSH16;

            for (byte x = (byte)OpCode.PUSH1; x <= (byte)OpCode.PUSH16; x++)
            {
                _jumpTable[x] = PUSHM1_PUSH16;
            }

            for (byte x = (byte)OpCode.JMP; x <= (byte)OpCode.JMPIFNOT; x++)
            {
                _jumpTable[x] = JMP_JMPIF_JMPIFNOT;
            }

            _jumpTable[(byte)OpCode.CALL] = CALL;
            _jumpTable[(byte)OpCode.RET] = RET;
            _jumpTable[(byte)OpCode.SYSCALL] = SYSCALL;
            _jumpTable[(byte)OpCode.NOP] = (e, c, i) => { return true; };

            return _jumpTable;
        }

        public bool PUSH0(ExecutionEngine engine, ExecutionContext context, Instruction instruction)
        {
            context.EvaluationStack.Push(EmptyBytes);
            if (!engine.CheckStackSize(true)) return false;
            return true;
        }

        public bool PUSHM1_PUSH16(ExecutionEngine engine, ExecutionContext context, Instruction instruction)
        {
            context.EvaluationStack.Push((int)instruction.OpCode - (int)OpCode.PUSH1 + 1);
            if (!engine.CheckStackSize(true)) return false;
            return true;
        }

        public bool PUSHBYTES1_PUSHBYTES75(ExecutionEngine engine, ExecutionContext context, Instruction instruction)
        {
            if (!engine.CheckMaxItemSize(instruction.Operand.Length)) return false;
            context.EvaluationStack.Push(instruction.Operand);
            if (!engine.CheckStackSize(true)) return false;
            return true;
        }

        public bool JMP_JMPIF_JMPIFNOT(ExecutionEngine engine, ExecutionContext context, Instruction instruction)
        {
            int offset = context.InstructionPointer + instruction.TokenI16;
            if (offset < 0 || offset > context.Script.Length) return false;
            bool fValue = true;
            if (instruction.OpCode > OpCode.JMP)
            {
                engine.CheckStackSize(false, -1);
                fValue = context.EvaluationStack.Pop().GetBoolean();

                if (instruction.OpCode == OpCode.JMPIFNOT)
                    fValue = !fValue;
            }
            if (fValue)
                context.InstructionPointer = offset;
            else
                context.InstructionPointer += 3;
            return true;
        }

        public bool CALL(ExecutionEngine engine, ExecutionContext context, Instruction instruction)
        {
            ExecutionContext context_call = context.Clone();
            context_call.InstructionPointer = context.InstructionPointer + instruction.TokenI16;
            if (context_call.InstructionPointer < 0 || context_call.InstructionPointer > context_call.Script.Length) return false;

            engine.LoadContext(context_call);
            return true;
        }

        public bool RET(ExecutionEngine engine, ExecutionContext context, Instruction instruction)
        {
            ExecutionContext context_pop = engine.InvocationStack.Pop();
            int rvcount = context_pop.RVCount;
            if (rvcount == -1) rvcount = context_pop.EvaluationStack.Count;
            if (rvcount > 0)
            {
                if (context_pop.EvaluationStack.Count < rvcount) return false;
                RandomAccessStack<StackItem> stack_eval;
                if (engine.InvocationStack.Count == 0)
                    stack_eval = engine.ResultStack;
                else
                    stack_eval = engine.CurrentContext.EvaluationStack;
                context_pop.EvaluationStack.CopyTo(stack_eval, rvcount);
            }
            if (context_pop.RVCount == -1 && engine.InvocationStack.Count > 0)
            {
                context_pop.AltStack.CopyTo(engine.CurrentContext.AltStack);
            }
            engine.CheckStackSize(false, 0);
            engine.RaiseContextUnloaded(context_pop);
            if (engine.InvocationStack.Count == 0)
            {
                engine.State = VMState.HALT;
            }
            return true;
        }

        public bool SYSCALL(ExecutionEngine engine, ExecutionContext context, Instruction instruction)
        {
            if (!engine.OnSysCall(instruction.TokenU32) || !engine.CheckStackSize(false, int.MaxValue))
                return false;

            return true;
        }

        public bool DUPFROMALTSTACKBOTTOM(ExecutionEngine engine, ExecutionContext context, Instruction instruction)
        {
            var item = context.AltStack.Peek(context.AltStack.Count - 1);
            context.EvaluationStack.Push(item);
            if (!engine.CheckStackSize(true)) return false;
            return true;
        }

        public bool DUPFROMALTSTACK(ExecutionEngine engine, ExecutionContext context, Instruction instruction)
        {
            context.EvaluationStack.Push(context.AltStack.Peek());
            if (!engine.CheckStackSize(true)) return false;
            return true;
        }
    }
}