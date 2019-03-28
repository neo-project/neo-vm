using System;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
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
            get
            {
                return BitConverter.ToInt16(Operand, 0);
            }
        }

        public short TokenI16_1
        {
            get
            {
                return BitConverter.ToInt16(Operand, sizeof(short));
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
            int operandSize = 0;
            switch (OperandSizePrefixTable[(int)OpCode])
            {
                case 0:
                    operandSize = OperandSizeTable[(int)OpCode];
                    break;
                case 1:
                    operandSize = ReadByte(script, ref ip);
                    break;
                case 2:
                    operandSize = ReadUInt16(script, ref ip);
                    break;
                case 4:
                    operandSize = ReadInt32(script, ref ip);
                    break;
            }
            if (operandSize > 0)
                this.Operand = ReadBytes(script, ref ip, operandSize);
        }

        private static byte ReadByte(byte[] script, ref int ip)
        {
            if (ip + sizeof(byte) > script.Length)
                throw new InvalidOperationException();
            return script[ip++];
        }

        private static byte[] ReadBytes(byte[] script, ref int ip, int count)
        {
            if (ip + count > script.Length)
                throw new InvalidOperationException();
            byte[] buffer = new byte[count];
            Unsafe.MemoryCopy(script, ip, buffer, 0, count);
            ip += count;
            return buffer;
        }

        public byte[] ReadBytes(int offset, int count)
        {
            if (offset + count > Operand.Length)
                throw new InvalidOperationException();
            byte[] buffer = new byte[count];
            Unsafe.MemoryCopy(Operand, offset, buffer, 0, count);
            return buffer;
        }

        private static int ReadInt32(byte[] script, ref int ip)
        {
            if (ip + sizeof(int) > script.Length)
                throw new InvalidOperationException();
            int value = Unsafe.ToInt32(script, ip);
            ip += sizeof(int);
            return value;
        }

        private static ushort ReadUInt16(byte[] script, ref int ip)
        {
            if (ip + sizeof(ushort) > script.Length)
                throw new InvalidOperationException();
            ushort value = Unsafe.ToUInt16(script, ip);
            ip += sizeof(ushort);
            return value;
        }
    }
}
