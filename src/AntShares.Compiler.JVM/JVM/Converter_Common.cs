using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AntShares.Compiler.JVM
{
    public partial class ModuleConverter
    {
        private AntsCode _Insert1(AntShares.VM.OpCode code, string comment, AntsMethod to, byte[] data = null)
        {
            AntsCode _code = new AntsCode();
            int startaddr = addr;
            _code.addr = addr;
            {
                _code.debugcode = comment;
                _code.debugline = 0;
            }

            addr++;

            _code.code = code;

            if (data != null)
            {
                _code.bytes = data;
                addr += _code.bytes.Length;
            }
            to.body_Codes[startaddr] = _code;
            return _code;
        }

        private AntsCode _InsertPush(byte[] data, string comment, AntsMethod to)
        {
            if (data.Length == 0) return _Insert1(VM.OpCode.PUSH0, comment, to);
            if (data.Length <= 75) return _Insert1((VM.OpCode)data.Length, comment, to, data);
            byte prefixLen;
            VM.OpCode code;
            if (data.Length <= byte.MaxValue)
            {
                prefixLen = sizeof(byte);
                code = VM.OpCode.PUSHDATA1;
            }
            else if (data.Length <= ushort.MaxValue)
            {
                prefixLen = sizeof(ushort);
                code = VM.OpCode.PUSHDATA2;
            }
            else
            {
                prefixLen = sizeof(uint);
                code = VM.OpCode.PUSHDATA4;
            }
            byte[] bytes = new byte[data.Length + prefixLen];
            Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, bytes, 0, prefixLen);
            Buffer.BlockCopy(data, 0, bytes, prefixLen, data.Length);
            return _Insert1(code, comment, to, bytes);
        }

        private AntsCode _InsertPush(int i, string comment, AntsMethod to)
        {
            if (i == 0) return _Insert1(VM.OpCode.PUSH0, comment, to);
            if (i == -1) return _Insert1(VM.OpCode.PUSHM1, comment, to);
            if (i > 0 && i <= 16) return _Insert1((VM.OpCode)(byte)i + 0x50, comment, to);
            return _InsertPush(((BigInteger)i).ToByteArray(), comment, to);
        }

        private AntsCode _Convert1by1(AntShares.VM.OpCode code, OpCode src, AntsMethod to, byte[] data = null)
        {
            AntsCode _code = new AntsCode();
            int startaddr = addr;
            _code.addr = addr;
            if (src != null)
            {
                addrconv[src.addr] = addr;
                _code.debugcode = src.debugcode;
                _code.debugline = src.debugline;
                _code.debugILAddr = src.addr;
                _code.debugILCode = src.code.ToString();
            }


            addr++;

            _code.code = code;

            if (data != null)
            {
                _code.bytes = data;
                addr += _code.bytes.Length;
            }
            to.body_Codes[startaddr] = _code;
            return _code;
        }

        private AntsCode _ConvertPush(byte[] data, OpCode src, AntsMethod to)
        {
            if (data.Length == 0) return _Convert1by1(VM.OpCode.PUSH0, src, to);
            //if (data.Length <= 75) return _Convert1by1((VM.OpCode)data.Length, src, to, data);
            byte prefixLen;
            VM.OpCode code;
            if (data.Length <= byte.MaxValue)
            {
                prefixLen = sizeof(byte);
                code = VM.OpCode.PUSHDATA1;
            }
            else if (data.Length <= ushort.MaxValue)
            {
                prefixLen = sizeof(ushort);
                code = VM.OpCode.PUSHDATA2;
            }
            else
            {
                prefixLen = sizeof(uint);
                code = VM.OpCode.PUSHDATA4;
            }
            byte[] bytes = new byte[data.Length + prefixLen];
            Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, bytes, 0, prefixLen);
            Buffer.BlockCopy(data, 0, bytes, prefixLen, data.Length);
            return _Convert1by1(code, src, to, bytes);
        }

        private AntsCode _ConvertPush(long i, OpCode src, AntsMethod to)
        {
            if (i == 0) return _Convert1by1(VM.OpCode.PUSH0, src, to);
            if (i == -1) return _Convert1by1(VM.OpCode.PUSHM1, src, to);
            if (i > 0 && i <= 16) return _Convert1by1((VM.OpCode)(byte)i + 0x50, src, to);
            return _ConvertPush(((BigInteger)i).ToByteArray(), src, to);
        }

        private void _insertBeginCode(JavaMethod from, AntsMethod to)
        {
            //压入槽位栈
            _InsertPush(from.MaxVariableIndex, "begincode", to);
            _Insert1(VM.OpCode.NEWARRAY, "", to);
            _Insert1(AntShares.VM.OpCode.TOALTSTACK, "", to);

            for(var i=0;i<from.paramTypes.Count;i++)
            {
                _Insert1(VM.OpCode.DUPFROMALTSTACK, "init param:" + i, to);
                _InsertPush(from.argTable[i], "", to);
                _InsertPush(2, "", to);
                _Insert1(VM.OpCode.ROLL, "", to);
                _Insert1(VM.OpCode.SETITEM, "", to);
            }
            ////初始化临时槽位位置
            //to.addVariablesCount = from.addLocal_VariablesCount;
            //for (var i = 0; i < from.addLocal_VariablesCount; i++)
            //{
            //    //to.body_Variables.Add(new JavaParam(src.name, src.type));
            //    _InsertPush(0, "body_Variables init", to);
            //}
        }

        private void _insertEndCode(JavaMethod from, AntsMethod to, OpCode src)
        {
            //占位不谢
            //_Convert1by1(AntShares.VM.OpCode.NOP, src, to);

            ////移除临时槽位
            ////drop body_Variables
            //for (var i = 0; i < from.addLocal_VariablesCount; i++)
            //{
            //    _Insert1(AntShares.VM.OpCode.DEPTH, "body_Variables drop", to, null);
            //    _Insert1(AntShares.VM.OpCode.DEC, null, to, null);

            //    //push olddepth
            //    _Insert1(AntShares.VM.OpCode.FROMALTSTACK, null, to);
            //    _Insert1(AntShares.VM.OpCode.DUP, null, to);
            //    _Insert1(AntShares.VM.OpCode.TOALTSTACK, null, to);
            //    //(d-1)-olddepth
            //    _Insert1(AntShares.VM.OpCode.SUB, null, to);

            //    _Insert1(AntShares.VM.OpCode.XDROP, null, to, null);
            //}
            ////移除参数槽位
            //for (var i = 0; i < from.paramTypes.Count; i++)
            //{
            //    //d
            //    _Insert1(AntShares.VM.OpCode.DEPTH, "param drop", to, null);

            //    //push olddepth
            //    _Insert1(AntShares.VM.OpCode.FROMALTSTACK, null, to);
            //    _Insert1(AntShares.VM.OpCode.DUP, null, to);
            //    _Insert1(AntShares.VM.OpCode.DEC, null, to);//深度-1
            //    _Insert1(AntShares.VM.OpCode.TOALTSTACK, null, to);

            //    //(d)-olddepth
            //    _Insert1(AntShares.VM.OpCode.SUB, null, to);

            //    _Insert1(AntShares.VM.OpCode.XDROP, null, to, null);

            //}

            //移除深度临时栈
            _Insert1(AntShares.VM.OpCode.FROMALTSTACK, "", to);
            _Insert1(AntShares.VM.OpCode.DROP, "", to);
        }
    }
}
