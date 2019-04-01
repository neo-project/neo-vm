using System;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    public class Instruction
    {
        public static Instruction RET { get; } = new Instruction(OpCode.RET);

        public readonly OpCode OpCode;
        public readonly ReadOnlyMemory<byte> Operand;

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
            get
            {
#if NETCOREAPP
                return BitConverter.ToInt16(Operand.Span);
#else
                if (Operand.Length < sizeof(short))
                    throw new InvalidOperationException();
                unsafe
                {
                    fixed (byte* pbyte = Operand.Span)
                    {
                        return *(short*)pbyte;
                    }
                }
#endif
            }
        }

        public short TokenI16_1
        {
            get
            {
#if NETCOREAPP
                return BitConverter.ToInt16(Operand.Span.Slice(2, 2));
#else
                if (Operand.Length < sizeof(short) * 2)
                    throw new InvalidOperationException();
                unsafe
                {
                    fixed (byte* pbyte = &Operand.Span[sizeof(short)])
                    {
                        return *(short*)pbyte;
                    }
                }
#endif
            }
        }

        static Instruction()
        {
            OperandSizePrefixTable[(int)OpCode.PUSHDATA1] = 1;
            OperandSizePrefixTable[(int)OpCode.PUSHDATA2] = 2;
            OperandSizePrefixTable[(int)OpCode.PUSHDATA4] = 4;
            OperandSizePrefixTable[(int)OpCode.SYSCALL] = 1;
            for (int i = (int)OpCode.PUSHBYTES1; i <= (int)OpCode.PUSHBYTES75; i++)
                OperandSizeTable[i] = i;
            OperandSizeTable[(int)OpCode.JMP] = 2;
            OperandSizeTable[(int)OpCode.JMPIF] = 2;
            OperandSizeTable[(int)OpCode.JMPIFNOT] = 2;
            OperandSizeTable[(int)OpCode.CALL] = 2;
            OperandSizeTable[(int)OpCode.APPCALL] = 20;
            OperandSizeTable[(int)OpCode.TAILCALL] = 20;
            OperandSizeTable[(int)OpCode.CALL_I] = 4;
            OperandSizeTable[(int)OpCode.CALL_E] = 22;
            OperandSizeTable[(int)OpCode.CALL_ED] = 2;
            OperandSizeTable[(int)OpCode.CALL_ET] = 22;
            OperandSizeTable[(int)OpCode.CALL_EDT] = 2;
        }

        private Instruction(OpCode opcode)
        {
            this.OpCode = opcode;
        }

        internal Instruction(byte[] script, int ip)
        {
            this.OpCode = (OpCode)script[ip++];
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
                this.Operand = new ReadOnlyMemory<byte>(script, ip + operandSizePrefix, operandSize);
        }
    }
}
