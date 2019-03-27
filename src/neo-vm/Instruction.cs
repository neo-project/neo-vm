using System;

namespace Neo.VM
{
    public class Instruction
    {
        public static Instruction RET { get; } = new Instruction(OpCode.RET);

        public readonly OpCode OpCode;
        public readonly byte[] Operand;
        public readonly int Size;

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

        private Instruction(OpCode opcode)
        {
            this.OpCode = opcode;
            this.Size = 1;
        }

        internal Instruction(byte[] script, int ip)
        {
            this.OpCode = (OpCode)script[ip++];
            this.Size = 1;
            if (OpCode >= OpCode.PUSHBYTES1 && OpCode <= OpCode.PUSHBYTES75)
            {
                int length = (int)OpCode;
                Size += length;
                Operand = ReadBytes(script, ref ip, length);
            }
            else
                switch (OpCode)
                {
                    case OpCode.PUSHDATA1:
                    case OpCode.SYSCALL:
                        {
                            int length = ReadByte(script, ref ip);
                            Size += 1 + length;
                            Operand = ReadBytes(script, ref ip, length);
                            break;
                        }
                    case OpCode.PUSHDATA2:
                        {
                            int length = ReadUInt16(script, ref ip);
                            Size += 2 + length;
                            Operand = ReadBytes(script, ref ip, length);
                            break;
                        }
                    case OpCode.PUSHDATA4:
                        {
                            int length = ReadInt32(script, ref ip);
                            Size += 4 + length;
                            Operand = ReadBytes(script, ref ip, length);
                            break;
                        }
                    case OpCode.JMP:
                    case OpCode.JMPIF:
                    case OpCode.JMPIFNOT:
                    case OpCode.CALL:
                    case OpCode.CALL_ED:
                    case OpCode.CALL_EDT:
                        Operand = ReadBytes(script, ref ip, 2);
                        Size += 2;
                        break;
                    case OpCode.APPCALL:
                    case OpCode.TAILCALL:
                        Operand = ReadBytes(script, ref ip, 20);
                        Size += 20;
                        break;
                    case OpCode.CALL_I:
                        Operand = ReadBytes(script, ref ip, 4);
                        Size += 4;
                        break;
                    case OpCode.CALL_E:
                    case OpCode.CALL_ET:
                        Operand = ReadBytes(script, ref ip, 22);
                        Size += 22;
                        break;
                }
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
