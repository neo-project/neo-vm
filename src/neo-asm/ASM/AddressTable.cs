using System;
using System.Collections.Generic;

namespace Neo.Compiler.ASM
{
    internal class AddressTable
    {
        private readonly List<Instruction> itable = new List<Instruction>();
        private readonly Dictionary<string, Label> ltable = new Dictionary<string, Label>();

        public AddressTable(IEnumerable<Semanteme> semantemes)
        {
            foreach (Semanteme semanteme in semantemes)
            {
                if (semanteme is Instruction)
                {
                    Instruction instruction = (Instruction)semanteme;
                    instruction.Process();
                }
                semanteme.BaseAddress = itable.Count == 0 ? 0 : itable[itable.Count - 1].BaseAddress + (uint)itable[itable.Count - 1].Code.Length;
                if (semanteme is Instruction)
                {
                    itable.Add((Instruction)semanteme);
                }
                else
                {
                    Label label = (Label)semanteme;
                    if (ltable.ContainsKey(label.Name))
                        throw new CompilerException(label.LineNumber, "duplicate label");
                    ltable.Add(label.Name, label);
                }
            }
            foreach (Instruction instruction in itable)
            {
                switch (instruction.Name)
                {
                    case InstructionName.JMP:
                    case InstructionName.JMPIF:
                    case InstructionName.JMPIFNOT:
                    case InstructionName.CALL:
                        if (!ltable.ContainsKey(instruction.Arguments[0]))
                            throw new CompilerException(instruction.LineNumber, "invalid label");
                        int offset = (int)ltable[instruction.Arguments[0]].BaseAddress - (int)instruction.BaseAddress;
                        if (offset < short.MinValue || offset > short.MaxValue)
                            throw new CompilerException(instruction.LineNumber, "long jump");
                        instruction.Code = instruction.ProcessJump((short)offset);
                        break;
                }
            }
        }

        public byte[] ToScript()
        {
            if (itable.Count == 0) return new byte[0];
            byte[] script = new byte[itable[itable.Count - 1].BaseAddress + itable[itable.Count - 1].Code.Length];
            foreach (Instruction instruction in itable)
                Buffer.BlockCopy(instruction.Code, 0, script, (int)instruction.BaseAddress, instruction.Code.Length);
            return script;
        }
    }
}
