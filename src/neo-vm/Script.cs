using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    [DebuggerDisplay("Length={Length}")]
    public class Script
    {
        private readonly byte[] _value;
        private readonly bool strictMode;
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
        public Script(byte[] script) : this(script, false)
        {
        }

        public Script(byte[] script, bool strictMode)
        {
            this._value = script;
            if (strictMode)
            {
                for (int ip = 0; ip < script.Length; ip += GetInstruction(ip).Size) { }
                foreach (var (ip, instruction) in _instructions)
                {
                    switch (instruction.OpCode)
                    {
                        case OpCode.JMP:
                        case OpCode.JMPIF:
                        case OpCode.JMPIFNOT:
                        case OpCode.JMPEQ:
                        case OpCode.JMPNE:
                        case OpCode.JMPGT:
                        case OpCode.JMPGE:
                        case OpCode.JMPLT:
                        case OpCode.JMPLE:
                        case OpCode.CALL:
                        case OpCode.ENDTRY:
                            if (!_instructions.ContainsKey(checked(ip + instruction.TokenI8)))
                                throw new BadScriptException($"ip: {ip}, opcode: {instruction.OpCode}");
                            break;
                        case OpCode.PUSHA:
                        case OpCode.JMP_L:
                        case OpCode.JMPIF_L:
                        case OpCode.JMPIFNOT_L:
                        case OpCode.JMPEQ_L:
                        case OpCode.JMPNE_L:
                        case OpCode.JMPGT_L:
                        case OpCode.JMPGE_L:
                        case OpCode.JMPLT_L:
                        case OpCode.JMPLE_L:
                        case OpCode.CALL_L:
                        case OpCode.ENDTRY_L:
                            if (!_instructions.ContainsKey(checked(ip + instruction.TokenI32)))
                                throw new BadScriptException($"ip: {ip}, opcode: {instruction.OpCode}");
                            break;
                        case OpCode.TRY:
                            if (!_instructions.ContainsKey(checked(ip + instruction.TokenI8)))
                                throw new BadScriptException($"ip: {ip}, opcode: {instruction.OpCode}");
                            if (!_instructions.ContainsKey(checked(ip + instruction.TokenI8_1)))
                                throw new BadScriptException($"ip: {ip}, opcode: {instruction.OpCode}");
                            break;
                        case OpCode.TRY_L:
                            if (!_instructions.ContainsKey(checked(ip + instruction.TokenI32)))
                                throw new BadScriptException($"ip: {ip}, opcode: {instruction.OpCode}");
                            if (!_instructions.ContainsKey(checked(ip + instruction.TokenI32_1)))
                                throw new BadScriptException($"ip: {ip}, opcode: {instruction.OpCode}");
                            break;
                        case OpCode.NEWARRAY_T:
                        case OpCode.ISTYPE:
                        case OpCode.CONVERT:
                            StackItemType type = (StackItemType)instruction.TokenU8;
                            if (!Enum.IsDefined(typeof(StackItemType), type))
                                throw new BadScriptException();
                            if (instruction.OpCode != OpCode.NEWARRAY_T && type == StackItemType.Any)
                                throw new BadScriptException($"ip: {ip}, opcode: {instruction.OpCode}");
                            break;
                    }
                }
            }
            this.strictMode = strictMode;
        }

        public Instruction GetInstruction(int ip)
        {
            if (ip >= Length) return Instruction.RET;
            if (!_instructions.TryGetValue(ip, out Instruction instruction))
            {
                if (strictMode) throw new ArgumentException($"ip not found with strict mode", nameof(ip));
                instruction = new Instruction(_value, ip);
                _instructions.Add(ip, instruction);
            }
            return instruction;
        }

        public static implicit operator byte[](Script script) => script._value;
        public static implicit operator Script(byte[] script) => new Script(script);
    }
}
