using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Neo.VM
{
    [DebuggerDisplay("OpCode={OpCode}")]
    public class Instruction
    {
        public static Instruction RET { get; } = new Instruction(OpCode.RET);

        public readonly OpCode OpCode;
        public readonly ReadOnlyMemory<byte> Operand;

        private static readonly OperandSizeAttribute[] OperandSizeTable = new OperandSizeAttribute[256];

        public int Size
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var entry = OperandSizeTable[(byte)OpCode];
                return entry.SizePrefix > 0
                    ? 1 + entry.SizePrefix + Operand.Length
                    : 1 + entry.Size;
            }
        }

        public short TokenI16
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return BinaryPrimitives.ReadInt16LittleEndian(Operand.Span);
            }
        }

        public int TokenI32
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return BinaryPrimitives.ReadInt32LittleEndian(Operand.Span);
            }
        }

        public int TokenI32_1
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return BinaryPrimitives.ReadInt32LittleEndian(Operand.Span[4..]);
            }
        }

        public sbyte TokenI8
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (sbyte)Operand.Span[0];
            }
        }

        public sbyte TokenI8_1
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (sbyte)Operand.Span[1];
            }
        }

        public string TokenString
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Encoding.ASCII.GetString(Operand.Span);
            }
        }

        public ushort TokenU16
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return BinaryPrimitives.ReadUInt16LittleEndian(Operand.Span);
            }
        }

        public uint TokenU32
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return BinaryPrimitives.ReadUInt32LittleEndian(Operand.Span);
            }
        }

        public byte TokenU8
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Operand.Span[0];
            }
        }

        public byte TokenU8_1
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Operand.Span[1];
            }
        }

        static Instruction()
        {
            foreach (FieldInfo field in typeof(OpCode).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                OperandSizeAttribute attribute = field.GetCustomAttribute<OperandSizeAttribute>();
                if (attribute == null) continue;

                int index = (int)(OpCode)field.GetValue(null);
                OperandSizeTable[index] = attribute;
            }

            // Add default values in order to prevent the null checks

            for (int x = 0; x < OperandSizeTable.Length; x++)
            {
                if (OperandSizeTable[x] == null)
                {
                    OperandSizeTable[x] = new OperandSizeAttribute();
                }
            }
        }

        private Instruction(OpCode opcode)
        {
            OpCode = opcode;
        }

        internal Instruction(byte[] script, int ip)
        {
            OpCode = (OpCode)script[ip++];
            var entry = OperandSizeTable[(byte)OpCode];
            int operandSize = entry.GetOperandSize(script, ip);
            if (operandSize > 0)
            {
                ip += entry.SizePrefix;
                if (ip + operandSize > script.Length)
                    throw new InvalidOperationException();
                Operand = new ReadOnlyMemory<byte>(script, ip, operandSize);
            }
        }
    }
}
