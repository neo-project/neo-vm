using System;
using System.Linq;

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
            _Convert1by1(AntShares.VM.OpCode.PUSHDATA1, null, to, int2Pushdata1bytes(pos));
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
            _Convert1by1(AntShares.VM.OpCode.PUSHDATA1, null, to, int2Pushdata1bytes(pos));
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
            _Convert1by1(AntShares.VM.OpCode.PUSHDATA1, null, to, int2Pushdata1bytes(to.paramtypes.Count - 1 - pos));
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
                    if (attr.AttributeType.Name == "ScriptOpAttribute" || attr.AttributeType.Name == "OpCodeAttribute")
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

            if (this.outModule.mapMethods.ContainsKey(src.tokenMethod))
            {//this is a call
                var c = _Convert1by1(AntShares.VM.OpCode.CALL, src, to, new byte[] { 5, 0 });
                c.needfix = true;
                c.srcfunc = src.tokenMethod;
                return 0;
            }
            else if (refs.ReturnType.Name == "ScriptEngine" || refs.ReturnType.Name == "ExecutionEngine")
            {
                //donothing 語法過渡類型
                return 0;
            }
            else
            {//maybe a syscall // or other
                if (src.tokenMethod == "System.Int32 System.Numerics.BigInteger::op_Explicit(System.Numerics.BigInteger)")
                {
                    //donothing
                    return 0;
                }
                else if (src.tokenMethod == "System.Numerics.BigInteger System.Numerics.BigInteger::op_Implicit(System.Int32)")//int->bignumber
                {
                    //donothing
                    return 0;
                }
                else if(src.tokenMethod == "System.Numerics.BigInteger System.Numerics.BigInteger::op_Implicit(System.Int64)")
                {
                    return 0;
                }
                else if(src.tokenMethod == "System.Boolean System.Object::Equals(System.Object)")
                {
                    _Convert1by1(AntShares.VM.OpCode.EQUAL, src, to);
                    return 0;
                }
                else if(src.tokenMethod== "System.Numerics.BigInteger System.Numerics.BigInteger::op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger)")
                {
                    _Convert1by1(AntShares.VM.OpCode.ADD, src, to);
                    return 0;
                }
                else if(src.tokenMethod== "System.Numerics.BigInteger System.Numerics.BigInteger::op_Subtraction(System.Numerics.BigInteger,System.Numerics.BigInteger)")
                {
                    _Convert1by1(AntShares.VM.OpCode.SUB, src, to);
                    return 0;
                }
                else if(src.tokenMethod== "System.Numerics.BigInteger System.Numerics.BigInteger::op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger)")
                {
                    _Convert1by1(AntShares.VM.OpCode.MUL, src, to);
                    return 0;
                }
                else if(src.tokenMethod== "System.Boolean System.Numerics.BigInteger::op_LessThanOrEqual(System.Numerics.BigInteger,System.Int64)")
                {
                    _Convert1by1(AntShares.VM.OpCode.LTE, src, to);
                    return 0;
                }
                else if(src.tokenMethod== "System.Boolean System.Numerics.BigInteger::op_LessThan(System.Numerics.BigInteger,System.Numerics.BigInteger)")
                {
                    _Convert1by1(AntShares.VM.OpCode.LT, src, to);
                    return 0;
                }
                else if(src.tokenMethod== "System.Boolean System.Numerics.BigInteger::op_GreaterThan(System.Numerics.BigInteger,System.Numerics.BigInteger)")
                {
                    _Convert1by1(AntShares.VM.OpCode.GT, src, to);
                    return 0;
                }
                else
                {
                   
                    string name;
                    if (IsOpCall(refs, out name))
                    {
                        if (name == "CHECKSIG")
                        {
                            _Convert1by1(AntShares.VM.OpCode.CHECKSIG, src, to);
                            return 0;
                        }
                    }
                    if (IsSysCall(refs, out name))
                    {
                        _Convert1by1(AntShares.VM.OpCode.SYSCALL, src, to,str2Pushdata1bytes(name));
                        return 0;
                    }
                    throw new Exception("unknown call:" + src.tokenMethod);

                }
            }
        }

        private int _ConvertNewArr(ILMethod method, OpCode src, AntsMethod to)
        {
            var code = to.body_Codes.Last().Value;
            //we need a number
            if (code.code != AntShares.VM.OpCode.PUSHDATA1)
            {
                this.logger.Log("_ConvertNewArr::not support var lens for array.");
                return 0;
            }
            var number = pushdata1bytes2int(code.bytes);

            //移除上一条指令
            to.body_Codes.Remove(code.addr);
            this.addr--;
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
                    this._Convert1by1(AntShares.VM.OpCode.PUSHDATA1, src, to, toPushdata1bytes(data));

                    return 3;

                }
                else
                {
                    this._Convert1by1(AntShares.VM.OpCode.PUSHDATA1, src, to, toPushdata1bytes(new byte[number]));
                }
            }



            return 0;

        }
    }
}
