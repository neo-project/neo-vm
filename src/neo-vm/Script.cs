using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    [DebuggerDisplay("Length={Length}")]
    public class Script
    {
        private readonly byte[] _value;
        private readonly Dictionary<int, Instruction> _instructions = new Dictionary<int, Instruction>();

        /// <summary>
        /// Script length
        /// </summary>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _value.Length;
            }
        }

        /// <summary>
        /// Get opcode
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Returns the opcode</returns>
        public OpCode this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (OpCode)_value[index];
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="script">Script</param>
        public Script(byte[] script)
        {
            _value = script;
        }

        public Instruction GetInstruction(int ip)
        {
            if (ip >= Length) return Instruction.RET;
            if (!_instructions.TryGetValue(ip, out Instruction instruction))
            {
                instruction = new Instruction(_value, ip);
                _instructions.Add(ip, instruction);
            }
            return instruction;
        }

        public static implicit operator byte[](Script script) => script._value;
        public static implicit operator Script(byte[] script) => new Script(script);
    }
}
