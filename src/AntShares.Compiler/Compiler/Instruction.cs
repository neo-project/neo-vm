using AntShares.VM;
using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace AntShares.Compiler
{
    internal class Instruction : Semanteme
    {
        private const string ERR_INCORRECT_NUMBER = "incorrect number of arguments";
        private const string ERR_INVALID_ARGUMENT = "invalid argument";
        private const string ERR_SYNTAX_ERROR = "syntax error";

        public InstructionName Name;
        public string[] Arguments;
        public byte[] Code;

        private byte MakeScriptOp()
        {
            return (byte)(ScriptOp)Enum.Parse(typeof(ScriptOp), "OP_" + Name);
        }

        private byte[] ParseHex(string hex)
        {
            if (hex == null || hex.Length == 0)
                return new byte[0];
            if (hex.Length % 2 == 1)
                throw new CompilerException(LineNumber, ERR_INVALID_ARGUMENT);
            if (hex.StartsWith("0x")) hex = hex.Substring(2);
            byte[] result = new byte[hex.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier);
            return result;
        }

        public void Process()
        {
            switch (Name)
            {
                case InstructionName.PUSH:
                    Code = ProcessPush();
                    break;
                case InstructionName.NOP:
                case InstructionName.RET:
                case InstructionName.HALTIFNOT:
                case InstructionName.HALT:
                case InstructionName.TOALTSTACK:
                case InstructionName.FROMALTSTACK:
                case InstructionName.XDROP:
                case InstructionName.XSWAP:
                case InstructionName.XTUCK:
                case InstructionName.DEPTH:
                case InstructionName.DROP:
                case InstructionName.DUP:
                case InstructionName.NIP:
                case InstructionName.OVER:
                case InstructionName.PICK:
                case InstructionName.ROLL:
                case InstructionName.ROT:
                case InstructionName.SWAP:
                case InstructionName.TUCK:
                case InstructionName.CAT:
                case InstructionName.SUBSTR:
                case InstructionName.LEFT:
                case InstructionName.RIGHT:
                case InstructionName.SIZE:
                case InstructionName.INVERT:
                case InstructionName.AND:
                case InstructionName.OR:
                case InstructionName.XOR:
                case InstructionName.EQUAL:
                case InstructionName.NEGATE:
                case InstructionName.ABS:
                case InstructionName.NOT:
                case InstructionName.ADD:
                case InstructionName.SUB:
                case InstructionName.MUL:
                case InstructionName.DIV:
                case InstructionName.MOD:
                case InstructionName.LSHIFT:
                case InstructionName.RSHIFT:
                case InstructionName.BOOLAND:
                case InstructionName.BOOLOR:
                case InstructionName.NUMEQUAL:
                case InstructionName.NUMNOTEQUAL:
                case InstructionName.LESSTHAN:
                case InstructionName.GREATERTHAN:
                case InstructionName.LESSTHANOREQUAL:
                case InstructionName.GREATERTHANOREQUAL:
                case InstructionName.MIN:
                case InstructionName.MAX:
                case InstructionName.WITHIN:
                case InstructionName.SHA1:
                case InstructionName.SHA256:
                case InstructionName.HASH160:
                case InstructionName.HASH256:
                case InstructionName.CHECKSIG:
                case InstructionName.CHECKMULTISIG:
                    Code = ProcessOthers();
                    break;
                case InstructionName.JMP:
                case InstructionName.JMPIF:
                case InstructionName.JMPIFNOT:
                case InstructionName.CALL:
                    Code = ProcessJump();
                    break;
                case InstructionName.APPCALL:
                    Code = ProcessAppCall();
                    break;
                case InstructionName.SYSCALL:
                    Code = ProcessSysCall();
                    break;
                case InstructionName.INC:
                    Code = ProcessInc();
                    break;
                case InstructionName.DEC:
                    Code = ProcessDec();
                    break;
                case InstructionName.MUL2:
                    Code = ProcessMul2();
                    break;
                case InstructionName.DIV2:
                    Code = ProcessDiv2();
                    break;
                case InstructionName.NOTZERO:
                    Code = ProcessNotZero();
                    break;
                default:
                    throw new CompilerException(LineNumber, ERR_SYNTAX_ERROR);
            }
        }

        private byte[] ProcessAppCall()
        {
            if (Arguments.Length != 1) throw new CompilerException(LineNumber, ERR_INCORRECT_NUMBER);
            byte[] hash = ParseHex(Arguments[0]);
            if (hash.Length != 20) throw new CompilerException(LineNumber, ERR_INVALID_ARGUMENT);
            byte[] result = new byte[21];
            result[0] = (byte)ScriptOp.OP_APPCALL;
            Buffer.BlockCopy(hash, 0, result, 1, 20);
            return result;
        }

        private byte[] ProcessDec()
        {
            if (Arguments.Length != 0) throw new CompilerException(LineNumber, ERR_INCORRECT_NUMBER);
            return new[] { (byte)ScriptOp.OP_1SUB };
        }

        private byte[] ProcessDiv2()
        {
            if (Arguments.Length != 0) throw new CompilerException(LineNumber, ERR_INCORRECT_NUMBER);
            return new[] { (byte)ScriptOp.OP_2DIV };
        }

        private byte[] ProcessInc()
        {
            if (Arguments.Length != 0) throw new CompilerException(LineNumber, ERR_INCORRECT_NUMBER);
            return new[] { (byte)ScriptOp.OP_1ADD };
        }

        private byte[] ProcessMul2()
        {
            if (Arguments.Length != 0) throw new CompilerException(LineNumber, ERR_INCORRECT_NUMBER);
            return new[] { (byte)ScriptOp.OP_2MUL };
        }

        private byte[] ProcessNotZero()
        {
            if (Arguments.Length != 0) throw new CompilerException(LineNumber, ERR_INCORRECT_NUMBER);
            return new[] { (byte)ScriptOp.OP_0NOTEQUAL };
        }

        internal byte[] ProcessJump(short offset = 0)
        {
            if (Arguments.Length != 1) throw new CompilerException(LineNumber, ERR_INCORRECT_NUMBER);
            byte[] data = BitConverter.GetBytes(offset);
            byte[] result = new byte[3];
            result[0] = MakeScriptOp();
            Buffer.BlockCopy(data, 0, result, 1, sizeof(short));
            return result;
        }

        private byte[] ProcessOthers()
        {
            if (Arguments.Length != 0) throw new CompilerException(LineNumber, ERR_INCORRECT_NUMBER);
            return new[] { MakeScriptOp() };
        }

        private byte[] ProcessPush()
        {
            if (Arguments.Length != 1) throw new CompilerException(LineNumber, ERR_INCORRECT_NUMBER);
            BigInteger bi;
            if (BigInteger.TryParse(Arguments[0], out bi))
                using (ScriptBuilder sb = new ScriptBuilder())
                    return sb.Push(bi).ToArray();
            else if (string.Compare(Arguments[0], "true", true) == 0)
                return new[] { (byte)ScriptOp.OP_TRUE };
            else if (string.Compare(Arguments[0], "false", true) == 0)
                return new[] { (byte)ScriptOp.OP_FALSE };
            else if (Arguments[0].StartsWith("0x"))
                using (ScriptBuilder sb = new ScriptBuilder())
                    return sb.Push(ParseHex(Arguments[0])).ToArray();
            else
                throw new CompilerException(LineNumber, ERR_INVALID_ARGUMENT);
        }

        private byte[] ProcessSysCall()
        {
            if (Arguments.Length != 1) throw new CompilerException(LineNumber, ERR_INCORRECT_NUMBER);
            byte[] data = Encoding.ASCII.GetBytes(Arguments[0]);
            if (data.Length > 252) throw new CompilerException(LineNumber, ERR_INVALID_ARGUMENT);
            byte[] result = new byte[data.Length + 2];
            result[0] = (byte)ScriptOp.OP_SYSCALL;
            result[1] = (byte)data.Length;
            Buffer.BlockCopy(data, 0, result, 2, data.Length);
            return result;
        }
    }
}
