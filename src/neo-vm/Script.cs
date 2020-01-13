using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    [DebuggerDisplay("Length={Length}")]
    public class Script
    {
        private int _hashCode = -1;
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

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (!(obj is Script script)) return false;
            return _value.AsSpan().SequenceEqual(script._value);
        }

        public unsafe override int GetHashCode()
        {
            if (_hashCode == -1)
            {
                unchecked
                {
                    _hashCode = 17;
                    foreach (byte element in _value)
                        _hashCode = _hashCode * 31 + element;
                }
            }
            return _hashCode;
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
