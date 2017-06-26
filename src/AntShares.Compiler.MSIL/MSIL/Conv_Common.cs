using System;
using System.Numerics;

namespace AntShares.Compiler.MSIL
{
    /// <summary>
    /// 从ILCode 向小蚁 VM 转换的转换器
    /// </summary>
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
            if (data.Length <= 75) return _Convert1by1((VM.OpCode)data.Length, src, to, data);
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

        private void _insertBeginCode(ILMethod from, AntsMethod to)
        {
            ////压入深度临时栈
            //_Insert1(AntShares.VM.OpCode.DEPTH, "record depth.", to);
            //_Insert1(AntShares.VM.OpCode.TOALTSTACK, "", to);

            ////初始化临时槽位位置
            //foreach (var src in from.body_Variables)
            //{
            //    to.body_Variables.Add(new ILParam(src.name, src.type));
            //    _InsertPush(0, "body_Variables init", to);
            //}

            //新玩法，用一个数组，应该能减少指令数量
            _InsertPush(from.paramtypes.Count + from.body_Variables.Count, "begincode", to);
            _Insert1(AntShares.VM.OpCode.NEWARRAY, "", to);
            _Insert1(AntShares.VM.OpCode.TOALTSTACK, "", to);
            //移动参数槽位
            for (var i = 0; i < from.paramtypes.Count; i++)
            {
                //getarray
                _Insert1(AntShares.VM.OpCode.FROMALTSTACK, "set param:" + i, to);
                _Insert1(AntShares.VM.OpCode.DUP, null, to);
                _Insert1(AntShares.VM.OpCode.TOALTSTACK, null, to);

                _InsertPush(i, "", to); //Array pos

                _InsertPush(2, "", to); //Array item
                _Insert1(AntShares.VM.OpCode.ROLL, null, to);

                _Insert1(AntShares.VM.OpCode.SETITEM, null, to);
            }
        }

        private void _insertEndCode(ILMethod from, AntsMethod to, OpCode src)
        {
            ////占位不谢
            _Convert1by1(AntShares.VM.OpCode.NOP, src, to);

            ////移除临时槽位
            ////drop body_Variables
            //for (var i = 0; i < from.body_Variables.Count; i++)
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
            //for (var i = 0; i < from.paramtypes.Count; i++)
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
            _Insert1(AntShares.VM.OpCode.FROMALTSTACK, "endcode", to);
            _Insert1(AntShares.VM.OpCode.DROP, "", to);
        }

    }
}
