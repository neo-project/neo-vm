using Neo.VM;
using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace Neo.Compiler.ASM
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
            return (byte)(OpCode)Enum.Parse(typeof(OpCode), Name.ToString());
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
                case InstructionName.DUPFROMALTSTACK:
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
                case InstructionName.INC:
                case InstructionName.DEC:
                case InstructionName.SIGN:
                case InstructionName.NEGATE:
                case InstructionName.ABS:
                case InstructionName.NOT:
                case InstructionName.NZ:
                case InstructionName.ADD:
                case InstructionName.SUB:
                case InstructionName.MUL:
                case InstructionName.DIV:
                case InstructionName.MOD:
                case InstructionName.SHL:
                case InstructionName.SHR:
                case InstructionName.BOOLAND:
                case InstructionName.BOOLOR:
                case InstructionName.NUMEQUAL:
                case InstructionName.NUMNOTEQUAL:
                case InstructionName.LT:
                case InstructionName.GT:
                case InstructionName.LTE:
                case InstructionName.GTE:
                case InstructionName.MIN:
                case InstructionName.MAX:
                case InstructionName.WITHIN:
                case InstructionName.SHA1:
                case InstructionName.SHA256:
                case InstructionName.HASH160:
                case InstructionName.HASH256:
                case InstructionName.CHECKSIG:
                case InstructionName.CHECKMULTISIG:
                case InstructionName.ARRAYSIZE:
                case InstructionName.PACK:
                case InstructionName.UNPACK:
                case InstructionName.PICKITEM:
                case InstructionName.SETITEM:
                case InstructionName.NEWARRAY:
                case InstructionName.NEWSTRUCT:
                case InstructionName.THROW:
                case InstructionName.THROWIFNOT:
                    Code = ProcessOthers();
                    break;
                case InstructionName.JMP:
                case InstructionName.JMPIF:
                case InstructionName.JMPIFNOT:
                case InstructionName.CALL:
                    Code = ProcessJump();
                    break;
                case InstructionName.APPCALL:
                case InstructionName.TAILCALL:
                    Code = ProcessAppCall();
                    break;
                case InstructionName.SYSCALL:
                    Code = ProcessSysCall();
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
            result[0] = MakeScriptOp();
            Buffer.BlockCopy(hash, 0, result, 1, 20);
            return result;
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
                    return sb.EmitPush(bi).ToArray();
            else if (string.Compare(Arguments[0], "true", true) == 0)
                return new[] { (byte)OpCode.PUSHT };
            else if (string.Compare(Arguments[0], "false", true) == 0)
                return new[] { (byte)OpCode.PUSHF };
            else if (Arguments[0].StartsWith("0x"))
                using (ScriptBuilder sb = new ScriptBuilder())
                    return sb.EmitPush(ParseHex(Arguments[0])).ToArray();
            else
                throw new CompilerException(LineNumber, ERR_INVALID_ARGUMENT);
        }

        private byte[] ProcessSysCall()
        {
            if (Arguments.Length != 1) throw new CompilerException(LineNumber, ERR_INCORRECT_NUMBER);
            byte[] data = Encoding.ASCII.GetBytes(Arguments[0]);
            if (data.Length > 252) throw new CompilerException(LineNumber, ERR_INVALID_ARGUMENT);
            byte[] result = new byte[data.Length + 2];
            result[0] = (byte)OpCode.SYSCALL;
            result[1] = (byte)data.Length;
            Buffer.BlockCopy(data, 0, result, 2, data.Length);
            return result;
        }
    }
}
