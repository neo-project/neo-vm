using AntShares.Compiler.JVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntShares.Compiler.JVM
{
    public partial class ModuleConverter
    {
        private void _ConvertStLoc(OpCode src, AntsMethod to, int pos)
        {
            //push d
            var c = _Convert1by1(AntShares.VM.OpCode.DUPFROMALTSTACK, src, to);
            if (c.debugcode == null)
            {
                c.debugcode = "from StLoc -> 6 code";
                c.debugline = 0;
            }
            _InsertPush(pos, "", to);//add index

            _InsertPush(2, "", to);
            _Insert1(VM.OpCode.ROLL, "", to);
            _Insert1(VM.OpCode.SETITEM, "", to);
        }
        private void _ConvertLdLoc(OpCode src, AntsMethod to, int pos)
        {
            //push d
            var c = _Convert1by1(AntShares.VM.OpCode.DUPFROMALTSTACK, src, to);
            if (c.debugcode == null)
            {
                c.debugcode = "from LdLoc -> 5 code";
                c.debugline = 0;
            }
            _InsertPush(pos, "", to);//add index


            //pick
            _Convert1by1(AntShares.VM.OpCode.PICKITEM, null, to);
        }
        private void _ConvertLdLocA(OpCode src, AntsMethod to, int pos)
        {
            _ConvertPush(pos, src, to);
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

        public bool IsOpCall(JavaMethod method, OpCode src, out string callname)
        {
            if (method != null)
                if (method.method.Annotations != null)
                {

                    object[] op = method.method.Annotations[0] as object[];
                    if (op[1] as string == "LAntShares/SmartContract/Framework/OpCode;")
                    {
                        if (op[2] as string == "value")
                        {
                            var info = op[3] as object[];
                            callname = info[2] as string;
                            return true;
                        }


                    }
                }


            //m.Annotations

            callname = "";
            return false;
        }
        public bool IsSysCall(JavaMethod method, OpCode src, out string callname)
        {
            if (method != null)
                if (method.method.Annotations != null)
                {

                    object[] op = method.method.Annotations[0] as object[];
                    if (op[1] as string == "LAntShares/SmartContract/Framework/Syscall;")
                    {
                        if (op[2] as string == "value")
                        {
                            var info = op[3] as string;
                            callname = info;
                            return true;
                        }


                    }
                }


            //m.Annotations

            callname = "";
            return false;
        }
        public bool IsAppCall(JavaMethod method, OpCode src, out byte[] callhash)
        {
            if (method != null)
                if (method.method.Annotations != null)
                {

                    object[] op = method.method.Annotations[0] as object[];
                    if (op[1] as string == "LAntShares/SmartContract/Framework/Appcall;")
                    {
                        if (op[2] as string == "HexStr")
                        {
                            var info = op[3] as string;
                            byte[] bytes = new byte[info.Length / 2];

                            for (var i = 0; i < info.Length / 2; i++)
                            {
                                bytes[i] = byte.Parse(info.Substring(i * 2, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                            }
                            callhash = bytes;
                            return true;
                        }


                    }
                }


            //m.Annotations

            callhash = null;
            return false;
        }
        private int _ConvertCall(JavaMethod method, OpCode src, AntsMethod to)
        {
            _Convert1by1(VM.OpCode.NOP, src, to);
            var cc = method.DeclaringType.classfile.constantpool;
            var c = cc[src.arg1] as javaloader.ClassFile.ConstantPoolItemMethodref;
            var name = c.Class + "::" + c.Name;

            List<string> paramTypes = new List<string>();
            string returntype;
            JavaMethod.scanTypes(c.Signature, out returntype, paramTypes);


            JavaClass javaclass = null;
            JavaMethod _javamethod = null;

            if (this.srcModule.classes.ContainsKey(c.Class))
            {
                javaclass = this.srcModule.classes[c.Class];
                if (javaclass.methods.ContainsKey(c.Name))
                {
                    _javamethod = javaclass.methods[c.Name];
                }
                else
                {
                    while (javaclass != null)
                    {
                        if (this.srcModule.classes.ContainsKey(javaclass.superClass))
                        {
                            javaclass = this.srcModule.classes[javaclass.superClass];
                            if (javaclass.methods.ContainsKey(c.Name))
                            {
                                _javamethod = javaclass.methods[c.Name];
                                break;
                            }
                        }
                        else
                        {
                            javaclass = null;
                        }
                    }
                }
            }
            int calltype = 0;
            string callname = "";
            byte[] callhash = null;
            VM.OpCode callcode = VM.OpCode.NOP;

            if (IsOpCall(_javamethod, src, out callname))
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
            else if (IsSysCall(_javamethod, src, out callname))
            {
                calltype = 3;
            }
            else if (IsAppCall(_javamethod, src, out callhash))
            {
                calltype = 4;
            }
            else if (this.outModule.mapMethods.ContainsKey(name))
            {//this is a call
                calltype = 1;
            }
            else
            {

                if (name == "java.io.PrintStream::println")
                {//drop 1;
                    Console.WriteLine("logstr.");
                    _Convert1by1(VM.OpCode.DROP, src, to);
                    return 0;
                }
                else if (name == "java.math.BigInteger::add")
                {
                    _Convert1by1(VM.OpCode.ADD, src, to);
                    return 0;
                }
                else if (name == "java.math.BigInteger::multiply")
                {
                    _Convert1by1(VM.OpCode.MUL, src, to);
                    return 0;
                }
                else if (name == "java.math.BigInteger::divide")
                {
                    _Convert1by1(VM.OpCode.DIV, src, to);
                    return 0;
                }
                else if (name == "java.math.BigInteger::mod")
                {
                    _Convert1by1(VM.OpCode.MOD, src, to);
                    return 0;
                }
                else if (name == "java.math.BigInteger::compareTo")
                {
                    //need parse
                    _Convert1by1(VM.OpCode.SUB, src, to);
                    //_Convert1by1(VM.OpCode.DEC, src, to);
                    return 0;
                }
                else if (name == "java.math.BigInteger::equals" ||
                    name == "java.lang.String::equals")
                {
                    _Convert1by1(VM.OpCode.NUMEQUAL, src, to);
                    //_Convert1by1(VM.OpCode.DEC, src, to);
                    return 0;
                }
                else if (name == "java.math.BigInteger::valueOf" ||
                    name == "java.math.BigInteger::intValue" ||
                    name == "java.lang.Boolean::valueOf" ||
                    name == "java.lang.Character::valueOf"||
                    name == "java.lang.String::valueOf")
                {
                    //donothing
                    return 0;
                }
                else if (name == "java.lang.Boolean::booleanValue")
                {
                    _Convert1by1(VM.OpCode.NOP, src, to);
                    return 0;
                }
                else if (name == "java.lang.String::hashCode")
                {
                    //java switch 的编译方式很奇怪
                    return 0;
                }
                else if (name == "java.lang.String::charAt")
                {
                    _ConvertPush(1, src, to);
                    _Convert1by1(AntShares.VM.OpCode.SUBSTR, null, to);
                    return 0;
                }
                else if(name== "java.lang.String::length")
                {
                    _Convert1by1(AntShares.VM.OpCode.SIZE, null, to);
                    return 0;
                }
                else if(c.Class== "java.lang.StringBuilder")
                {
                    return _ConvertStringBuilder(c.Name, null, to);
                }
            }

            if (calltype == 0)
            {
                throw new Exception("unknown call:" + name);
            }
            var pcount = paramTypes.Count;

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
                var _c = _Convert1by1(AntShares.VM.OpCode.CALL, null, to, new byte[] { 5, 0 });
                _c.needfix = true;
                _c.srcfunc = name;
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
                if (bytes.Length > 252) throw new Exception("string is too long");
                byte[] outbytes = new byte[bytes.Length + 1];
                outbytes[0] = (byte)bytes.Length;
                Array.Copy(bytes, 0, outbytes, 1, bytes.Length);
                //bytes.Prepend 函数在 dotnet framework 4.6 编译不过
                _Convert1by1(AntShares.VM.OpCode.SYSCALL, null, to, outbytes);
                return 0;
            }
            else if (calltype == 4)
            {
                _Convert1by1(AntShares.VM.OpCode.APPCALL, null, to, callhash);

            }
            return 0;
        }

        private int _ConvertNewArray(JavaMethod method, OpCode src, AntsMethod to)
        {
            int skipcount = 0;
            if (src.arg1 != 8)
            {
                //this.logger.Log("_ConvertNewArray::not support type " + src.arg1 + " for array.");
                _Convert1by1(VM.OpCode.NEWARRAY, src, to);
                return 0;
            }
            //bytearray
            var code = to.body_Codes.Last().Value;
            //we need a number
            if (code.code > AntShares.VM.OpCode.PUSH16)
            {
                throw new Exception("_ConvertNewArr::not support var lens for new byte[?].");
            }
            var number = getNumber(code);

            //移除上一条指令
            to.body_Codes.Remove(code.addr);
            this.addr--;

            OpCode next = src;
            int dupcount = 0;
            int pcount = 0;
            int[] buf = new int[] { 0, 0, 0 };
            byte[] outbuf = new byte[number];
            do
            {
                int n = method.GetNextCodeAddr(next.addr);
                next = method.body_Codes[n];
                if (next.code == javaloader.NormalizedByteCode.__dup)
                {
                    dupcount++;
                    skipcount++;
                }
                else if (next.code == javaloader.NormalizedByteCode.__iconst)
                {
                    buf[pcount] = next.arg1;
                    pcount++;
                    skipcount++;
                }
                else if (next.code == javaloader.NormalizedByteCode.__bastore)
                {
                    dupcount--;
                    var v = (byte)buf[pcount - 1];
                    var i = buf[pcount - 2];
                    //while (outbuf.Count <= i)
                    //    outbuf.Add(0);
                    outbuf[i] = v;
                    pcount -= 2;
                    skipcount++;
                }
                else if (next.code == javaloader.NormalizedByteCode.__astore)
                {
                    _ConvertPush(outbuf.ToArray(), src, to);
                    return skipcount;
                }
                else
                {
                    throw new Exception("can not parse this new array code chain.");
                }
            }
            while (next != null);

            return 0;
        }
        private int _ConvertNew(JavaMethod method, OpCode src, AntsMethod to)
        {
            var c =            method.DeclaringType.classfile.constantpool[src.arg1] as javaloader.ClassFile.ConstantPoolItemClass;
            if(c.Name== "java.lang.StringBuilder")
            {
                _ConvertPush(1, src, to);
                _Insert1(VM.OpCode.NEWARRAY, "", to);
            }
            else
            {
                throw new Exception("new not supported type." + c.Name);
            }
            return 0;
        }
        private int _ConvertStringBuilder(string callname, OpCode src, AntsMethod to)
        {
            if(callname=="<init>")
            {
                _Convert1by1(VM.OpCode.SWAP, null, to);
                _Convert1by1(VM.OpCode.DUP, null, to);

                _ConvertPush(0, null, to);
                _ConvertPush(3, null, to);
                _Convert1by1(VM.OpCode.ROLL, null, to);
                _Convert1by1(VM.OpCode.SETITEM, null, to);
                return 0;
            }
            if(callname=="append")
            {
                _Convert1by1(VM.OpCode.SWAP, null, to);//把对象数组换上来
                _Convert1by1(VM.OpCode.DUP, null, to);
                _ConvertPush(0, null, to);
                _Convert1by1(VM.OpCode.PICKITEM, null, to);

                _ConvertPush(2, null, to);
                _Convert1by1(VM.OpCode.ROLL,null,to);
                _Convert1by1(VM.OpCode.SWAP, null, to);//把对象数组换上来
                _Convert1by1(VM.OpCode.CAT, null, to);

                _ConvertPush(0, null, to);
                _Convert1by1(VM.OpCode.SWAP, null, to);//把对象数组换上来
                _Convert1by1(VM.OpCode.SETITEM, null, to);
                return 0;
            }
            if(callname== "toString")
            {
                _ConvertPush(0, null, to);
                _Convert1by1(VM.OpCode.PICKITEM, null, to);
                return 0;
            }
            return 0;
        }
        //private int _ConvertNewArr(ILMethod method, OpCode src, AntsMethod to)
        //{
        //    var code = to.body_Codes.Last().Value;
        //    //we need a number
        //    if (code.code > AntShares.VM.OpCode.PUSH16)
        //    {
        //        this.logger.Log("_ConvertNewArr::not support var lens for array.");
        //        return 0;
        //    }
        //    var number = getNumber(code);

        //    //移除上一条指令
        //    to.body_Codes.Remove(code.addr);
        //    this.addr--;
        //    if (code.bytes != null)
        //        this.addr -= code.bytes.Length;

        //    var type = src.tokenType;
        //    if (type != "System.Byte")
        //    {
        //        this.logger.Log("_ConvertNewArr::not support type " + type + " for array.");
        //    }
        //    else
        //    {
        //        int n = method.GetNextCodeAddr(src.addr);
        //        int n2 = method.GetNextCodeAddr(n);
        //        int n3 = method.GetNextCodeAddr(n2);
        //        if (method.body_Codes[n].code == CodeEx.Dup && method.body_Codes[n2].code == CodeEx.Ldtoken && method.body_Codes[n3].code == CodeEx.Call)
        //        {//這是在初始化數組

        //            var data = method.body_Codes[n2].tokenUnknown as byte[];
        //            this._ConvertPush(data, src, to);

        //            return 3;

        //        }
        //        else
        //        {
        //            this._ConvertPush(new byte[number], src, to);
        //        }
        //    }



        //    return 0;

        //}
        //private int _ConvertInitObj(OpCode src, AntsMethod to)
        //{
        //    var type = (src.tokenUnknown as Mono.Cecil.TypeReference).Resolve();
        //    _Convert1by1(AntShares.VM.OpCode.NOP, src, to);//空白
        //    _ConvertPush(type.Fields.Count, null, to);//插入个数量
        //    _Insert1(VM.OpCode.ARRAYNEW, null, to);
        //    //然後要將計算棧上的第一個值，寫入第二個值對應的pos
        //    _Convert1by1(AntShares.VM.OpCode.SWAP, null, to);//replace n to top

        //    //push d
        //    _Convert1by1(AntShares.VM.OpCode.DEPTH, null, to);

        //    _Convert1by1(AntShares.VM.OpCode.DEC, null, to);//d 多了一位，剪掉
        //    _Convert1by1(AntShares.VM.OpCode.SWAP, null, to);//把n拿上來
        //    //push n
        //    //_ConvertPush(pos, null, to);有n了
        //    //d-n-1
        //    _Convert1by1(AntShares.VM.OpCode.SUB, null, to);
        //    _Convert1by1(AntShares.VM.OpCode.DEC, null, to);

        //    //push olddepth
        //    _Convert1by1(AntShares.VM.OpCode.FROMALTSTACK, null, to);
        //    _Convert1by1(AntShares.VM.OpCode.DUP, null, to);
        //    _Convert1by1(AntShares.VM.OpCode.TOALTSTACK, null, to);
        //    //(d-n-1)-olddepth
        //    _Convert1by1(AntShares.VM.OpCode.SUB, null, to);

        //    //swap d-n-1 and top
        //    _Convert1by1(AntShares.VM.OpCode.XSWAP, null, to);
        //    //drop top
        //    _Convert1by1(AntShares.VM.OpCode.DROP, null, to);
        //    return 0;
        //}
        //private int _ConvertStfld(OpCode src, AntsMethod to)
        //{
        //    var field = (src.tokenUnknown as Mono.Cecil.FieldReference).Resolve();
        //    var type = field.DeclaringType;
        //    var id = type.Fields.IndexOf(field);
        //    if (id < 0)
        //        throw new Exception("impossible.");
        //    _Convert1by1(AntShares.VM.OpCode.NOP, src, to);//空白

        //    _Convert1by1(AntShares.VM.OpCode.SWAP, null, to);//把n拿上來 n 和 item
        //    //push d
        //    _Convert1by1(AntShares.VM.OpCode.DEPTH, src, to);
        //    _Convert1by1(AntShares.VM.OpCode.DEC, null, to);//d 多了一位，剪掉
        //    _Convert1by1(AntShares.VM.OpCode.SWAP, null, to);//把n拿上來

        //    //push n
        //    //_ConvertPush(pos, null, to);有n了
        //    //d-n-1
        //    _Convert1by1(AntShares.VM.OpCode.SUB, null, to);
        //    _Convert1by1(AntShares.VM.OpCode.DEC, null, to);

        //    //push olddepth
        //    _Convert1by1(AntShares.VM.OpCode.FROMALTSTACK, null, to);
        //    _Convert1by1(AntShares.VM.OpCode.DUP, null, to);
        //    _Convert1by1(AntShares.VM.OpCode.TOALTSTACK, null, to);
        //    //(d-n-1)-olddepth
        //    _Convert1by1(AntShares.VM.OpCode.SUB, null, to);

        //    //pick
        //    _Convert1by1(AntShares.VM.OpCode.PICK, null, to);


        //    _Convert1by1(AntShares.VM.OpCode.SWAP, null, to);//把item 拿上來 
        //    _ConvertPush(id, null, to);
        //    _Convert1by1(AntShares.VM.OpCode.ARRAYSETITEM, null, to);//修改值
        //    return 0;
        //}

        //private int _ConvertLdfld(OpCode src, AntsMethod to)
        //{
        //    var field = (src.tokenUnknown as Mono.Cecil.FieldReference).Resolve();
        //    var type = field.DeclaringType;
        //    var id = type.Fields.IndexOf(field);
        //    if (id < 0)
        //        throw new Exception("impossible.");
        //    _ConvertPush(id, src, to);
        //    _Convert1by1(AntShares.VM.OpCode.PICKITEM, null, to);//修改值

        //    return 0;
        //}
    }

}
