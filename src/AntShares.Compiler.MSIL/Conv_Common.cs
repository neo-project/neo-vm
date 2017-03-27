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


        private void _insertBeginCode(ILMethod from, AntsMethod to)
        {
            //压入深度临时栈
            _Insert1(AntShares.VM.OpCode.DEPTH, "record depth.", to);
            _Insert1(AntShares.VM.OpCode.TOALTSTACK, "", to);

            //初始化临时槽位位置
            foreach (var src in from.body_Variables)
            {
                to.body_Variables.Add(new ILParam(src.name, src.type));
                _Insert1(AntShares.VM.OpCode.PUSHDATA1, "body_Variables init", to, int2Pushdata1bytes(0));
            }
        }

        private void _insertEndCode(ILMethod from, AntsMethod to, OpCode src)
        {
            //占位不谢
            _Convert1by1(AntShares.VM.OpCode.NOP, src, to);

            //移除临时槽位
            //drop body_Variables
            for (var i = 0; i < from.body_Variables.Count; i++)
            {
                _Insert1(AntShares.VM.OpCode.DEPTH, "body_Variables drop", to, null);
                _Insert1(AntShares.VM.OpCode.DEC, null, to, null);

                //push olddepth
                _Insert1(AntShares.VM.OpCode.FROMALTSTACK, null, to);
                _Insert1(AntShares.VM.OpCode.DUP, null, to);
                _Insert1(AntShares.VM.OpCode.TOALTSTACK, null, to);
                //(d-1)-olddepth
                _Insert1(AntShares.VM.OpCode.SUB, null, to);

                _Insert1(AntShares.VM.OpCode.XDROP, null, to, null);
            }
            //移除参数槽位
            for (var i = 0; i < from.paramtypes.Count; i++)
            {
                //d
                _Insert1(AntShares.VM.OpCode.DEPTH, "param drop", to, null);

                //push olddepth
                _Insert1(AntShares.VM.OpCode.FROMALTSTACK, null, to);
                _Insert1(AntShares.VM.OpCode.DUP, null, to);
                _Insert1(AntShares.VM.OpCode.DEC, null, to);//深度-1
                _Insert1(AntShares.VM.OpCode.TOALTSTACK, null, to);

                //(d)-olddepth
                _Insert1(AntShares.VM.OpCode.SUB, null, to);

                _Insert1(AntShares.VM.OpCode.XDROP, null, to, null);

            }

            //移除深度临时栈
            _Insert1(AntShares.VM.OpCode.FROMALTSTACK, "", to);
            _Insert1(AntShares.VM.OpCode.DROP, "", to);
        }

    }
}
