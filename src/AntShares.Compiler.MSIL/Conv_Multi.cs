using System;
using System.Linq;
using System.Text;

namespace AntShares.Compiler.MSIL
{
    /// <summary>
    /// 从ILCode 向小蚁 VM 转换的转换器
    /// </summary>
    public partial class ModuleConverter
    {
        private void _ConvertStLoc(OpCode src, AntsMethod to, int pos)
        {
            //push d
            var c = _Convert1by1(AntShares.VM.OpCode.DEPTH, src, to);
            if (c.debugcode == null)
            {
                c.debugcode = "from StLoc -> 6 code";
                c.debugline = 0;
            }


            //_Convert1by1(AntShares.VM.ScriptOp.OP_DUP, src, to);
            //push n
            _ConvertPush(pos, null, to);
            //d-n-1
            _Convert1by1(AntShares.VM.OpCode.SUB, null, to);
            _Convert1by1(AntShares.VM.OpCode.DEC, null, to);

            //push olddepth
            _Convert1by1(AntShares.VM.OpCode.FROMALTSTACK, null, to);
            _Convert1by1(AntShares.VM.OpCode.DUP, null, to);
            _Convert1by1(AntShares.VM.OpCode.TOALTSTACK, null, to);
            //(d-n-1)-olddepth
            _Convert1by1(AntShares.VM.OpCode.SUB, null, to);

            //swap d-n-1 and top
            _Convert1by1(AntShares.VM.OpCode.XSWAP, null, to);
            //drop top
            _Convert1by1(AntShares.VM.OpCode.DROP, null, to);

        }
        private void _ConvertLdLoc(OpCode src, AntsMethod to, int pos)
        {
            //push d
            var c = _Convert1by1(AntShares.VM.OpCode.DEPTH, src, to);
            if (c.debugcode == null)
            {
                c.debugcode = "from LdLoc -> 5 code";
                c.debugline = 0;
            }
            //push n
            _ConvertPush(pos, null, to);
            //d-n-1
            _Convert1by1(AntShares.VM.OpCode.SUB, null, to);
            _Convert1by1(AntShares.VM.OpCode.DEC, null, to);

            //push olddepth
            _Convert1by1(AntShares.VM.OpCode.FROMALTSTACK, null, to);
            _Convert1by1(AntShares.VM.OpCode.DUP, null, to);
            _Convert1by1(AntShares.VM.OpCode.TOALTSTACK, null, to);
            //(d-n-1)-olddepth
            _Convert1by1(AntShares.VM.OpCode.SUB, null, to);

            //pick
            _Convert1by1(AntShares.VM.OpCode.PICK, null, to);
        }

        private void _ConvertLdArg(OpCode src, AntsMethod to, int pos)
        {
            //push d
            var c = _Convert1by1(AntShares.VM.OpCode.DEPTH, src, to);
            if (c.debugcode == null)
            {
                c.debugcode = "from LdArg -> 5 code";
                c.debugline = 0;
            }
            //push n
            _ConvertPush(pos, null, to);//翻转取参数顺序
            //_Convert1by1(AntShares.VM.OpCode.PUSHDATA1, null, to, int2Pushdata1bytes(to.paramtypes.Count - 1 - pos));
            //d+n
            _Convert1by1(AntShares.VM.OpCode.ADD, null, to);

            //push olddepth
            _Convert1by1(AntShares.VM.OpCode.FROMALTSTACK, null, to);
            _Convert1by1(AntShares.VM.OpCode.DUP, null, to);
            _Convert1by1(AntShares.VM.OpCode.TOALTSTACK, null, to);
            //(d+n)-olddepth
            _Convert1by1(AntShares.VM.OpCode.SUB, null, to);

            //pick
            _Convert1by1(AntShares.VM.OpCode.PICK, null, to);
        }

        public bool IsSysCall(Mono.Cecil.MethodReference refs, out string name)
        {
            try
            {
                var defs = refs.Resolve();
                foreach (var attr in defs.CustomAttributes)
                {
                    if (attr.AttributeType.Name == "SyscallAttribute")
                    {
                        var type = attr.ConstructorArguments[0].Type;
                        var value = (string)attr.ConstructorArguments[0].Value;

                        //dosth
                        name = value;
                        return true;



                    }
                    //if(attr.t)
                }
                name = "";
                return false;
            }
            catch
            {
                name = "";
                return false;
            }

        }
        public bool IsOpCall(Mono.Cecil.MethodReference refs, out string name)
        {
            try
            {
                var defs = refs.Resolve();
                foreach (var attr in defs.CustomAttributes)
                {
                    if (attr.AttributeType.Name == "OpCodeAttribute")
                    {
                        var type = attr.ConstructorArguments[0].Type;
                        var value = (byte)attr.ConstructorArguments[0].Value;

                        foreach (var t in type.Resolve().Fields)
                        {
                            if (t.Constant != null)
                            {
                                if ((byte)t.Constant == value)
                                {

                                    //dosth
                                    name = t.Name;
                                    return true;

                                }
                            }
                        }


                    }
                    //if(attr.t)
                }
                name = "";
                return false;
            }
            catch
            {
                name = "";
                return false;
            }
        }
        private int _ConvertCall(OpCode src, AntsMethod to)
        {
            Mono.Cecil.MethodReference refs = src.tokenUnknown as Mono.Cecil.MethodReference;

            int calltype = 0;
            string callname = "";
            VM.OpCode callcode = VM.OpCode.NOP;
            if (this.outModule.mapMethods.ContainsKey(src.tokenMethod))
            {//this is a call
                calltype = 1;
            }
            else if (refs.ReturnType.Name == "ExecutionEngine" || refs.ReturnType.Name == "Storage")
            {
                if (src != null)
                {
                    //有可能jump到此处
                    this.addrconv[src.addr] = this.addr;//因为没插入代码，实际是下一行
                }
                //donothing 語法過渡類型
                return 0;
            }
            else
            {//maybe a syscall // or other
                if (src.tokenMethod.Contains("::op_Explicit(") || src.tokenMethod.Contains("::op_Implicit("))
                {
                    //各类显示隐示转换都忽略
                    //有可能有一些会特殊处理，故还保留独立判断
                    if (src.tokenMethod == "System.Int32 System.Numerics.BigInteger::op_Explicit(System.Numerics.BigInteger)")
                    {
                        //donothing
                        return 0;
                    }
                    else if (src.tokenMethod == "System.Int64 System.Numerics.BigInteger::op_Explicit(System.Numerics.BigInteger)")
                    {
                        //donothing
                        return 0;
                    }
                    else if (src.tokenMethod == "System.Numerics.BigInteger System.Numerics.BigInteger::op_Implicit(System.Int32)")//int->bignumber
                    {
                        //donothing
                        return 0;
                    }
                    else if (src.tokenMethod == "System.Numerics.BigInteger System.Numerics.BigInteger::op_Implicit(System.Int64)")
                    {
                        return 0;
                    }

                    return 0;
                }
                else if (src.tokenMethod.Contains("::op_Equality(") || src.tokenMethod.Contains("::Equals("))
                {
                    //各类==指令
                    //有可能有一些会特殊处理，故还保留独立判断
                    if (src.tokenMethod == "System.Boolean System.String::op_Equality(System.String,System.String)")
                    {
                        _Convert1by1(AntShares.VM.OpCode.EQUAL, src, to);
                        return 0;
                    }
                    else if (src.tokenMethod == "System.Boolean System.Object::Equals(System.Object)")
                    {
                        _Convert1by1(AntShares.VM.OpCode.EQUAL, src, to);
                        return 0;
                    }
                    _Convert1by1(AntShares.VM.OpCode.EQUAL, src, to);
                    return 0;
                }
                else if(src.tokenMethod.Contains("::op_Inequality("))
                {
                    //各类!=指令
                    //有可能有一些会特殊处理，故还保留独立判断
                    if (src.tokenMethod == "System.Boolean System.Numerics.BigInteger::op_Inequality(System.Numerics.BigInteger,System.Numerics.BigInteger)")
                    {
                        _Convert1by1(VM.OpCode.INVERT, src, to);
                        _Insert1(AntShares.VM.OpCode.EQUAL, "", to);
                        return 0;
                    }
                    _Convert1by1(VM.OpCode.INVERT, src, to);
                    _Insert1(AntShares.VM.OpCode.EQUAL, "", to);
                    return 0;
                }
                else if (src.tokenMethod.Contains("::op_Addition(") )
                {
                    //各类+指令
                    //有可能有一些会特殊处理，故还保留独立判断
                    if (src.tokenMethod == "System.Numerics.BigInteger System.Numerics.BigInteger::op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger)")
                    {
                        _Convert1by1(AntShares.VM.OpCode.ADD, src, to);
                        return 0;
                    }
                    _Convert1by1(AntShares.VM.OpCode.ADD, src, to);
                    return 0;
                }
                else if (src.tokenMethod.Contains("::op_Subtraction("))
                {
                    //各类-指令
                    //有可能有一些会特殊处理，故还保留独立判断
                    if (src.tokenMethod == "System.Numerics.BigInteger System.Numerics.BigInteger::op_Subtraction(System.Numerics.BigInteger,System.Numerics.BigInteger)")
                    {
                        _Convert1by1(AntShares.VM.OpCode.SUB, src, to);
                        return 0;
                    }
                    _Convert1by1(AntShares.VM.OpCode.SUB, src, to);
                    return 0;
                }
                else if (src.tokenMethod.Contains("::op_Multiply("))
                {
                    //各类*指令
                    //有可能有一些会特殊处理，故还保留独立判断
                    if (src.tokenMethod == "System.Numerics.BigInteger System.Numerics.BigInteger::op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger)")
                    {
                        _Convert1by1(AntShares.VM.OpCode.MUL, src, to);
                        return 0;
                    }
                    _Convert1by1(AntShares.VM.OpCode.MUL, src, to);
                    return 0;
                }
                else if (src.tokenMethod.Contains("::op_Division("))
                {
                    //各类/指令
                    //有可能有一些会特殊处理，故还保留独立判断
                    if (src.tokenMethod == "System.Numerics.BigInteger System.Numerics.BigInteger::op_Division(System.Numerics.BigInteger, System.Numerics.BigInteger)")
                    {
                        _Convert1by1(AntShares.VM.OpCode.DIV, src, to);
                        return 0;
                    }
                    _Convert1by1(AntShares.VM.OpCode.DIV, src, to);
                    return 0;
                }
                else if (src.tokenMethod.Contains("::op_Modulus("))
                {
                    //各类%指令
                    //有可能有一些会特殊处理，故还保留独立判断
                    if (src.tokenMethod == "System.Numerics.BigInteger System.Numerics.BigInteger::op_Modulus(System.Numerics.BigInteger,System.Numerics.BigInteger)")
                    {
                        _Convert1by1(AntShares.VM.OpCode.MOD, src, to);
                        return 0;
                    }
                    _Convert1by1(AntShares.VM.OpCode.MOD, src, to);
                    return 0;
                }
                else if (src.tokenMethod.Contains("::op_LessThan("))
                {
                    //各类<指令
                    //有可能有一些会特殊处理，故还保留独立判断
                    _Convert1by1(AntShares.VM.OpCode.LT, src, to);
                    return 0;
                }
                else if (src.tokenMethod.Contains("::op_GreaterThan("))
                {
                    //各类>指令
                    //有可能有一些会特殊处理，故还保留独立判断
                    _Convert1by1(AntShares.VM.OpCode.GT, src, to);
                    return 0;
                }
                else if (src.tokenMethod.Contains("::op_LessThanOrEqual("))
                {
                    //各类<=指令
                    //有可能有一些会特殊处理，故还保留独立判断
                    _Convert1by1(AntShares.VM.OpCode.LTE, src, to);
                    return 0;
                }
                else if (src.tokenMethod.Contains("::op_GreaterThanOrEqual("))
                {
                    //各类>=指令
                    //有可能有一些会特殊处理，故还保留独立判断
                    _Convert1by1(AntShares.VM.OpCode.GTE, src, to);
                    return 0;
                }
                else if(src.tokenMethod.Contains("::get_Length("))
                {
                    //各类.Length指令
                    //"System.Int32 System.String::get_Length()"
                    _Convert1by1(AntShares.VM.OpCode.SIZE, src, to);
                    return 0;
                }
                else if(src.tokenMethod.Contains("::Concat("))
                {
                    //各类.Concat
                    //"System.String System.String::Concat(System.String,System.String)"
                    _Convert1by1(AntShares.VM.OpCode.CAT, src, to);
                    return 0;
                }
                else if(src.tokenMethod== "System.String System.String::Substring(System.Int32,System.Int32)")
                {
                    _Convert1by1(AntShares.VM.OpCode.SUBSTR, src, to);
                    return 0;

                }
                else if(src.tokenMethod== "System.String System.String::Substring(System.Int32)")
                {
                    throw new Exception("antsmachine cant use this call,please use  .SubString(1,2) with 2 params.");
                }
                else
                {
                    if (IsOpCall(refs, out callname))
                    {
                        if (System.Enum.TryParse<VM.OpCode>(callname, out callcode))
                        {
                            calltype = 2;
                        }
                        else
                        {
                            throw new Exception("Can not find OpCall:" + callname);
                        }
                    }
                    if (IsSysCall(refs, out callname))
                    {
                        calltype = 3;
                    }

                }
            }

            if (calltype == 0)
                throw new Exception("unknown call:" + src.tokenMethod);
            var md = src.tokenUnknown as Mono.Cecil.MethodReference;
            var pcount = md.Parameters.Count;
            _Convert1by1(VM.OpCode.NOP, src, to);
            if (pcount <= 1)
            {
            }
            else if (pcount == 2)
            {
                _Insert1(VM.OpCode.SWAP, "swap 2 param", to);
            }
            else if (pcount == 3)
            {
                _InsertPush(2, "swap 0 and 2 param", to);
                _Insert1(VM.OpCode.XSWAP, "", to);
            }
            else
            {
                for (var i = 0; i < pcount / 2; i++)
                {
                    int saveto = (pcount - 1 - i);
                    _InsertPush(saveto, "load" + saveto, to);
                    _Insert1(VM.OpCode.PICK, "", to);

                    _InsertPush(i + 1, "load" + i + 1, to);
                    _Insert1(VM.OpCode.PICK, "", to);


                    _InsertPush(saveto + 2, "save to" + saveto + 2, to);
                    _Insert1(VM.OpCode.XSWAP, "", to);
                    _Insert1(VM.OpCode.DROP, "", to);

                    _InsertPush(i + 1, "save to" + i + 1, to);
                    _Insert1(VM.OpCode.XSWAP, "", to);
                    _Insert1(VM.OpCode.DROP, "", to);

                }
            }

            if (calltype == 1)
            {
                var c = _Convert1by1(AntShares.VM.OpCode.CALL, null, to, new byte[] { 5, 0 });
                c.needfix = true;
                c.srcfunc = src.tokenMethod;
                return 0;
            }
            else if (calltype == 2)
            {
                _Convert1by1(callcode, null, to);
                return 0;
            }
            else if (calltype == 3)
            {
                var bytes = Encoding.UTF8.GetBytes(callname);
                if (bytes.Length > 252) throw new Exception("string is to long");
                byte[] outbytes = new byte[bytes.Length + 1];
                outbytes[0] = (byte)bytes.Length;
                Array.Copy(bytes, 0, outbytes, 1, bytes.Length);
                //bytes.Prepend 函数在 dotnet framework 4.6 编译不过
                _Convert1by1(AntShares.VM.OpCode.SYSCALL, null, to, outbytes);
                return 0;
            }
            return 0;
        }

        private int _ConvertNewArr(ILMethod method, OpCode src, AntsMethod to)
        {
            var code = to.body_Codes.Last().Value;
            //we need a number
            if (code.code > AntShares.VM.OpCode.PUSH16)
            {
                this.logger.Log("_ConvertNewArr::not support var lens for array.");
                return 0;
            }
            var number = getNumber(code);

            //移除上一条指令
            to.body_Codes.Remove(code.addr);
            this.addr--;
            if (code.bytes != null)
                this.addr -= code.bytes.Length;

            var type = src.tokenType;
            if (type != "System.Byte")
            {
                this.logger.Log("_ConvertNewArr::not support type " + type + " for array.");
            }
            else
            {
                int n = method.GetNextCodeAddr(src.addr);
                int n2 = method.GetNextCodeAddr(n);
                int n3 = method.GetNextCodeAddr(n2);
                if (method.body_Codes[n].code == CodeEx.Dup && method.body_Codes[n2].code == CodeEx.Ldtoken && method.body_Codes[n3].code == CodeEx.Call)
                {//這是在初始化數組

                    var data = method.body_Codes[n2].tokenUnknown as byte[];
                    this._ConvertPush(data, src, to);

                    return 3;

                }
                else
                {
                    this._ConvertPush(new byte[number], src, to);
                }
            }



            return 0;

        }
    }
}
