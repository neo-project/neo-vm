using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Neo.VM
{
    [DebuggerDisplay("OpCode={OpCode}")]
    public class Instruction
    {
        public static Instruction RET { get; } = new Instruction(OpCode.RET);

        public readonly OpCode OpCode;
        public readonly byte[] Operand;

        private static readonly int[] OperandSizePrefixTable = new int[256];
        private static readonly int[] OperandSizeTable = new int[256];

        public int Size
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                int prefixSize = OperandSizePrefixTable[(int)OpCode];
                return prefixSize > 0
                    ? 1 + prefixSize + Operand.Length
                    : 1 + OperandSizeTable[(int)OpCode];
            }
        }

        public short TokenI16
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return BitConverter.ToInt16(Operand, 0);
            }
        }

        public string TokenString
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Encoding.ASCII.GetString(Operand);
            }
        }

        public uint TokenU32
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return BitConverter.ToUInt32(Operand, 0);
            }
        }

        static Instruction()
        {
            OperandSizePrefixTable[(int)OpCode.PUSHDATA1] = 1;
            OperandSizePrefixTable[(int)OpCode.PUSHDATA2] = 2;
            OperandSizePrefixTable[(int)OpCode.PUSHDATA4] = 4;
            for (int i = (int)OpCode.PUSHBYTES1; i <= (int)OpCode.PUSHBYTES75; i++)
                OperandSizeTable[i] = i;
            OperandSizeTable[(int)OpCode.JMP] = 2;
            OperandSizeTable[(int)OpCode.JMPIF] = 2;
            OperandSizeTable[(int)OpCode.JMPIFNOT] = 2;
            OperandSizeTable[(int)OpCode.CALL] = 2;
            OperandSizeTable[(int)OpCode.SYSCALL] = 4;
        }

        private Instruction(OpCode opcode)
        {
            OpCode = opcode;
        }

        internal Instruction(byte[] script, int ip)
        {
            OpCode = (OpCode)script[ip++];
            int operandSizePrefix = OperandSizePrefixTable[(int)OpCode];
            int operandSize = 0;
            switch (operandSizePrefix)
            {
                case 0:
                    operandSize = OperandSizeTable[(int)OpCode];
                    break;
                case 1:
                    operandSize = script[ip];
                    break;
                case 2:
                    operandSize = BitConverter.ToUInt16(script, ip);
                    break;
                case 4:
                    operandSize = BitConverter.ToInt32(script, ip);
                    break;
            }
            if (operandSize > 0)
            {
                ip += operandSizePrefix;
                Operand = new byte[operandSize];
                if (ip + operandSize > script.Length)
                    throw new InvalidOperationException();
                Unsafe.MemoryCopy(script, ip, Operand, 0, operandSize);
            }
        }
    }
}
