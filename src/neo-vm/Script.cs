using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    [DebuggerDisplay("Length={Length}")]
    public class Script : IEqualityComparer<Script>
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (!(obj is Script script)) return false;
            return Equals(script, this);
        }

        public bool Equals(Script x, Script y)
        {
            if (x == null || y == null) return x == y;
            if (ReferenceEquals(x, y)) return true;

            return Unsafe.MemoryEquals(x._value, y._value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return GetHashCode(this);
        }

        public unsafe int GetHashCode(Script obj)
        {
            if (_hashCode != -1) return _hashCode;
            if (obj is null) return 0;

            int len = obj._value.Length;
            if (len == 0) return 0;

            _hashCode = 1;
            fixed (byte* xp = obj._value)
            {
                int* xlp = (int*)xp;
                for (; len >= 4; len -= 4)
                {
                    _hashCode *= *xlp;
                    xlp++;
                }
                byte* xbp = (byte*)xlp;
                for (; len > 0; len--)
                {
                    _hashCode *= *xbp;
                    xbp++;
                }
            }

            return _hashCode;
        }

        public static implicit operator byte[](Script script) => script._value;
        public static implicit operator Script(byte[] script) => new Script(script);
    }
}
