using AntShares.Compiler.JVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntShares.Compiler.JVM
{
    public class Converter
    {
        public static byte[] Convert(string classfilename, ILogger logger = null)
        {

            var moduleJVMPackage = new JavaModule();
            moduleJVMPackage.LoadClass("go.class");
            moduleJVMPackage.LoadJar("AntShares.SmartContract.Framework.jar");

            var converter = new ModuleConverter(logger);
            //有异常的话在 convert 函数中会直接throw 出来
            var antmodule = converter.Convert(moduleJVMPackage);
            return antmodule.Build();
        }

    }
    class DefLogger : ILogger
    {
        public void Log(string log)
        {
            Console.WriteLine(log);
        }
    }
    public partial class ModuleConverter
    {
        public ModuleConverter(ILogger logger)
        {
            if (logger == null)
            {
                logger = new DefLogger();
            }
            this.logger = logger;

        }

        ILogger logger;
        JavaModule srcModule;
        public AntsModule outModule;
        public Dictionary<JavaMethod, AntsMethod> methodLink = new Dictionary<JavaMethod, AntsMethod>();
        public AntsModule Convert(JavaModule _in)
        {
            this.srcModule = _in;
            //logger.Log("beginConvert.");
            this.outModule = new AntsModule(this.logger);
            foreach (var c in _in.classes.Values)
            {
                if (c.skip) continue;
                foreach (var m in c.methods)
                {
                    if (m.Value.skip) continue;
                    if (m.Key[0] == '<') continue;//系統函數不要
                    AntsMethod nm = new AntsMethod();
                    nm.name = c.classfile.Name + "::" + m.Key;
                    this.methodLink[m.Value] = nm;
                    outModule.mapMethods[nm.name] = nm;
                }
            }

            foreach (var c in _in.classes.Values)
            {
                if (c.skip) continue;
                foreach (var m in c.methods)
                {
                    if (m.Value.skip) continue;
                    if (m.Key[0] == '<') continue;//系統函數不要

                    var nm = this.methodLink[m.Value];
                    //try
                    {
                        this.ConvertMethod(m.Value, nm);
                    }
                    //catch (Exception err)
                    //{
                    //    logger.Log("error:" + err.Message);
                    //}
                }
            }
            //转换完了，做个link，全部拼到一起
            string mainmethod = "";
            foreach (var key in outModule.mapMethods.Keys)
            {
                if (key.Contains("Verify"))
                {
                    var m = outModule.mapMethods[key];
                    foreach (var l in this.methodLink)
                    {
                        if (l.Value == m)
                        {
                            var srcm = l.Key;
                            if (srcm.DeclaringType.superClass == "VerificationCode" && srcm.returnType == "System.Boolean")
                            {
                                logger.Log("找到函数入口点:" + key);
                                if (mainmethod != "")
                                    throw new Exception("拥有多个函数入口点，请检查");
                                mainmethod = key;

                            }
                        }
                    }
                }
                if (key.Contains("Main"))
                {
                    var m = outModule.mapMethods[key];
                    foreach (var l in this.methodLink)
                    {
                        if (l.Value == m)
                        {
                            var srcm = l.Key;
                            if (srcm.DeclaringType.superClass == "AntShares.SmartContract.Framework.FunctionCode")
                            {
                                logger.Log("找到函数入口点:" + key);
                                if (mainmethod != "")
                                    throw new Exception("拥有多个函数入口点，请检查");
                                mainmethod = key;

                            }
                        }
                    }
                }
            }
            if (mainmethod == "")
            {
                throw new Exception("找不到入口函数，请检查");

            }
            //得找到第一个函数
            this.LinkCode(mainmethod);
            //this.findFirstFunc();//得找到第一个函数
            //然后给每个method 分配一个func addr
            //还需要对所有的call 做一次地址转换

            //this.outModule.Build();
            return outModule;
        }
        Stack<string> convertType = new Stack<string>();
        private void LinkCode(string main)
        {
            if (this.outModule.mapMethods.ContainsKey(main) == false)
            {
                throw new Exception("找不到名为" + main + "的入口");
            }
            var first = this.outModule.mapMethods[main];
            first.funcaddr = 0;
            this.outModule.total_Codes.Clear();
            int addr = 0;
            foreach (var c in first.body_Codes)
            {
                if (addr != c.Key)
                {
                    throw new Exception("sth error");
                }
                this.outModule.total_Codes[addr] = c.Value;
                addr += 1;
                if (c.Value.bytes != null)
                    addr += c.Value.bytes.Length;
            }

            foreach (var m in this.outModule.mapMethods)
            {
                if (m.Key == main) continue;

                m.Value.funcaddr = addr;

                foreach (var c in m.Value.body_Codes)
                {
                    this.outModule.total_Codes[addr] = c.Value;
                    addr += 1;
                    if (c.Value.bytes != null)
                        addr += c.Value.bytes.Length;

                    //地址偏移
                    c.Value.addr += m.Value.funcaddr;
                }
            }

            foreach (var c in this.outModule.total_Codes.Values)
            {
                if (c.needfix)
                {//需要地址转换
                    var addrfunc = this.outModule.mapMethods[c.srcfunc].funcaddr;
                    Int16 addrconv = (Int16)(addrfunc - c.addr);
                    c.bytes = BitConverter.GetBytes(addrconv);
                }
            }
        }
        private void ConvertMethod(JavaMethod from, AntsMethod to)
        {
            convertType.Clear();
            to.returntype = from.returnType;
            for (var i = 0; i < from.paramTypes.Count; i++)
            {
                to.paramtypes.Add(new AntsParam("_" + i, from.paramTypes[i]));
            }



            this.addr = 0;
            this.addrconv.Clear();

            ////插入一个记录深度的代码，再往前的是参数
            _insertBeginCode(from, to);

            int skipcount = 0;
            foreach (var src in from.body_Codes.Values)
            {
                if (skipcount > 0)
                {
                    skipcount--;
                }
                else
                {
                    //在return之前加入清理参数代码
                    if (src.code == javaloader.NormalizedByteCode.__return || src.code == javaloader.NormalizedByteCode.__ireturn)//before return 
                    {
                        _insertEndCode(from, to, src);
                    }

                    skipcount = ConvertCode(from, src, to);
                }
            }

            ConvertAddrInMethod(to);
        }

        Dictionary<int, int> addrconv = new Dictionary<int, int>();
        int addr = 0;


        static int getNumber(AntsCode code)
        {
            if (code.code <= VM.OpCode.PUSHBYTES75)
                return (int)code.code;
            else if (code.code == VM.OpCode.PUSH0) return 0;
            else if (code.code == VM.OpCode.PUSH1) return 1;
            else if (code.code == VM.OpCode.PUSH2) return 2;
            else if (code.code == VM.OpCode.PUSH3) return 3;
            else if (code.code == VM.OpCode.PUSH4) return 4;
            else if (code.code == VM.OpCode.PUSH5) return 5;
            else if (code.code == VM.OpCode.PUSH6) return 6;
            else if (code.code == VM.OpCode.PUSH7) return 7;
            else if (code.code == VM.OpCode.PUSH8) return 8;
            else if (code.code == VM.OpCode.PUSH9) return 9;
            else if (code.code == VM.OpCode.PUSH10) return 10;
            else if (code.code == VM.OpCode.PUSH11) return 11;
            else if (code.code == VM.OpCode.PUSH12) return 12;
            else if (code.code == VM.OpCode.PUSH13) return 13;
            else if (code.code == VM.OpCode.PUSH14) return 14;
            else if (code.code == VM.OpCode.PUSH15) return 15;
            else if (code.code == VM.OpCode.PUSH16) return 16;
            else if (code.code == VM.OpCode.PUSHDATA1) return pushdata1bytes2int(code.bytes);
            else
                throw new Exception("not support getNumber From this:" + code.ToString());
        }
        static int pushdata1bytes2int(byte[] data)
        {
            var n = BitConverter.ToInt32(data, 1);
            return n;
        }
        private void ConvertAddrInMethod(AntsMethod to)
        {
            foreach (var c in to.body_Codes.Values)
            {
                if (c.needfix &&

                    c.code != AntShares.VM.OpCode.CALL //call 要做函数间的转换

                    )
                {

                    var addr = addrconv[c.srcaddr];
                    Int16 addroff = (Int16)(addr - c.addr);
                    c.bytes = BitConverter.GetBytes(addroff);
                    c.needfix = false;

                }
            }
        }

        private int ConvertCode(JavaMethod method, OpCode src, AntsMethod to)
        {
            int skipcount = 0;
            switch (src.code)
            {
                case javaloader.NormalizedByteCode.__nop:
                    _Convert1by1(AntShares.VM.OpCode.NOP, src, to);
                    break;
                case javaloader.NormalizedByteCode.__return:
                case javaloader.NormalizedByteCode.__ireturn:
                case javaloader.NormalizedByteCode.__lreturn:
                case javaloader.NormalizedByteCode.__freturn:
                case javaloader.NormalizedByteCode.__dreturn:
                case javaloader.NormalizedByteCode.__areturn:
                    //        //return 在外面特殊处理了
                    _Insert1(AntShares.VM.OpCode.RET, null, to);
                    break;

                case javaloader.NormalizedByteCode.__getstatic:
                    {
                        _Convert1by1(AntShares.VM.OpCode.NOP, src, to);

                        var cc = method.DeclaringType.classfile.constantpool;
                        var c = cc[src.arg1] as javaloader.ClassFile.ConstantPoolItemFieldref;
                        if (c.Class == "java.math.BigInteger")
                        {
                            if (c.Name == "ONE")
                            {
                                _ConvertPush(1, src, to);
                            }
                            if (c.Name == "ZERO")
                            {
                                _ConvertPush(0, src, to);
                            }

                        }
                        if (c.Class == "java.lang.System")
                        {
                            if (c.Name == "out")
                            {
                                //donothing
                            }
                        }
                        //this.convertType.Push(c.Signature);
                    }
                    break;
                case javaloader.NormalizedByteCode.__ldc:
                    {
                        var cc = method.DeclaringType.classfile.constantpool;
                        var item = cc[src.arg1];
                        if (item is javaloader.ClassFile.ConstantPoolItemString)
                        {
                            var str = (item as javaloader.ClassFile.ConstantPoolItemString).Value;
                            _ConvertPush(Encoding.UTF8.GetBytes(str), src, to);
                        }
                        else if (item is javaloader.ClassFile.ConstantPoolItemLong)
                        {
                            var v = (item as javaloader.ClassFile.ConstantPoolItemLong).Value;
                            _ConvertPush(v, src, to);
                        }
                        else if (item is javaloader.ClassFile.ConstantPoolItemInteger)
                        {
                            var v = (item as javaloader.ClassFile.ConstantPoolItemInteger).Value;
                            _ConvertPush(v, src, to);
                        }
                        else
                        {
                            throw new Exception("not parse.");
                        }
                    }
                    break;
                case javaloader.NormalizedByteCode.__iconst:

                    _ConvertPush(src.arg1, src, to);
                    break;
                case javaloader.NormalizedByteCode.__lconst_1:
                    _ConvertPush(1, src, to);
                    break;
                case javaloader.NormalizedByteCode.__lconst_0:
                    _ConvertPush(0, src, to);
                    break;
                case javaloader.NormalizedByteCode.__newarray:
                    skipcount = _ConvertNewArray(method, src, to);
                    break;

                case javaloader.NormalizedByteCode.__astore:
                case javaloader.NormalizedByteCode.__istore:
                    _ConvertStLoc(src, to, src.arg1);
                    break;
                case javaloader.NormalizedByteCode.__aload:
                case javaloader.NormalizedByteCode.__iload:
                    _ConvertLdLoc(src, to, src.arg1);
                    break;

                case javaloader.NormalizedByteCode.__invokevirtual:
                case javaloader.NormalizedByteCode.__invokestatic:
                    {
                        _ConvertCall(method, src, to);

                    }
                    break;
                case javaloader.NormalizedByteCode.__iinc:
                    _Convert1by1(VM.OpCode.NOP, src, to);
                    {
                        _Insert1(VM.OpCode.DUPFROMALTSTACK, "", to);
                        _InsertPush(src.arg1, "", to);
                        _Insert1(VM.OpCode.PICKITEM, "", to);
                        _InsertPush(src.arg2, "", to);
                        _Insert1(VM.OpCode.ADD, "", to);

                        _Insert1(VM.OpCode.DUPFROMALTSTACK, "", to);//array
                        _InsertPush(src.arg1, "", to);//index
                        _InsertPush(2, "", to);
                        _Insert1(VM.OpCode.ROLL, "", to);
                        _Insert1(VM.OpCode.SETITEM, "", to);

                    }
                    //_Convert1by1(VM.OpCode.INC, src, to);

                    break;


                //    case CodeEx.Ldloc_0:
                //        _ConvertLdLoc(src, to, 0);
                //        break;
                //    case CodeEx.Ldloc_1:
                //        _ConvertLdLoc(src, to, 1);
                //        break;
                //    case CodeEx.Ldloc_2:
                //        _ConvertLdLoc(src, to, 2);
                //        break;
                //    case CodeEx.Ldloc_3:
                //        _ConvertLdLoc(src, to, 3);
                //        break;
                //    case CodeEx.Ldloc_S:
                //        _ConvertLdLoc(src, to, src.tokenI32);
                //        break;

                //    case CodeEx.Ldarg_0:
                //        _ConvertLdArg(src, to, 0);
                //        break;
                //    case CodeEx.Ldarg_1:
                //        _ConvertLdArg(src, to, 1);
                //        break;
                //    case CodeEx.Ldarg_2:
                //        _ConvertLdArg(src, to, 2);
                //        break;
                //    case CodeEx.Ldarg_3:
                //        _ConvertLdArg(src, to, 3);
                //        break;
                //    case CodeEx.Ldarg_S:
                //    case CodeEx.Ldarg:
                //        _ConvertLdArg(src, to, src.tokenI32);
                //        break;
                //需要地址轉換的情況
                case javaloader.NormalizedByteCode.__goto:
                    {
                        var code = _Convert1by1(AntShares.VM.OpCode.JMP, src, to, new byte[] { 0, 0 });
                        code.needfix = true;
                        code.srcaddr = src.arg1 + src.addr;
                    }
                    break;

                //    case CodeEx.Br_S:
                //        {
                //            var code = _Convert1by1(AntShares.VM.OpCode.JMP, src, to, new byte[] { 0, 0 });
                //            code.needfix = true;
                //            code.srcaddr = src.tokenAddr_Index;
                //        }

                //        break;
                case javaloader.NormalizedByteCode.__if_icmpeq:
                    {
                        _Convert1by1(AntShares.VM.OpCode.NUMEQUAL, null, to);
                        var code = _Convert1by1(AntShares.VM.OpCode.JMPIF, null, to, new byte[] { 0, 0 });
                        code.needfix = true;
                        code.srcaddr = src.addr + src.arg1;
                    }
                    break;
                case javaloader.NormalizedByteCode.__if_icmpne:
                    {
                        _Convert1by1(AntShares.VM.OpCode.NUMNOTEQUAL, null, to);
                        var code = _Convert1by1(AntShares.VM.OpCode.JMPIF, null, to, new byte[] { 0, 0 });
                        code.needfix = true;
                        code.srcaddr = src.addr + src.arg1;
                    }
                    break;
                case javaloader.NormalizedByteCode.__ifne:
                    {
                        _ConvertPush(0, src, to);//和0比较
                        _Convert1by1(AntShares.VM.OpCode.NUMNOTEQUAL, null, to);
                        var code = _Convert1by1(AntShares.VM.OpCode.JMPIF, null, to, new byte[] { 0, 0 });
                        code.needfix = true;
                        code.srcaddr = src.arg1 + src.addr;
                    }
                    break;
                case javaloader.NormalizedByteCode.__ifeq:
                    {
                        _ConvertPush(0, src, to);//和0比较
                        _Convert1by1(AntShares.VM.OpCode.NUMEQUAL, null, to);
                        var code = _Convert1by1(AntShares.VM.OpCode.JMPIF, null, to, new byte[] { 0, 0 });
                        code.needfix = true;
                        code.srcaddr = src.arg1 + src.addr;
                    }
                    break;
                case javaloader.NormalizedByteCode.__iflt:
                    {
                        _ConvertPush(0, src, to);//和0比较
                        _Convert1by1(AntShares.VM.OpCode.GT, null, to);
                        var code = _Convert1by1(AntShares.VM.OpCode.JMPIF, null, to, new byte[] { 0, 0 });
                        code.needfix = true;
                        code.srcaddr = src.arg1 + src.addr;
                    }
                    break;
                case javaloader.NormalizedByteCode.__if_icmplt:
                    {
                        _Convert1by1(AntShares.VM.OpCode.LT, src, to);
                        var code = _Convert1by1(AntShares.VM.OpCode.JMPIF, null, to, new byte[] { 0, 0 });
                        code.needfix = true;
                        code.srcaddr = src.addr + src.arg1;
                    }
                    break;
                case javaloader.NormalizedByteCode.__ifle:
                    {
                        _ConvertPush(0, src, to);//和0比较
                        _Convert1by1(AntShares.VM.OpCode.GT, null, to);
                        var code = _Convert1by1(AntShares.VM.OpCode.JMPIF, null, to, new byte[] { 0, 0 });
                        code.needfix = true;
                        code.srcaddr = src.addr + src.arg1;
                    }
                    break;
                case javaloader.NormalizedByteCode.__if_icmple:
                    {
                        _Convert1by1(AntShares.VM.OpCode.LTE, src, to);
                        var code = _Convert1by1(AntShares.VM.OpCode.JMPIF, null, to, new byte[] { 0, 0 });
                        code.needfix = true;
                        code.srcaddr = src.addr + src.arg1;
                    }
                    break;
                case javaloader.NormalizedByteCode.__ifgt:
                    {
                        _ConvertPush(0, src, to);//和0比较
                        _Convert1by1(AntShares.VM.OpCode.LT, null, to);
                        var code = _Convert1by1(AntShares.VM.OpCode.JMPIF, null, to, new byte[] { 0, 0 });
                        code.needfix = true;
                        code.srcaddr = src.addr + src.arg1;
                    }
                    break;
                case javaloader.NormalizedByteCode.__if_icmpgt:
                    {
                        _Convert1by1(AntShares.VM.OpCode.GT, src, to);
                        var code = _Convert1by1(AntShares.VM.OpCode.JMPIF, null, to, new byte[] { 0, 0 });
                        code.needfix = true;
                        code.srcaddr = src.addr + src.arg1;
                    }
                    break;
                case javaloader.NormalizedByteCode.__ifge:
                    {
                        _ConvertPush(0, src, to);//和0比较
                        _Convert1by1(AntShares.VM.OpCode.LTE, null, to);
                        var code = _Convert1by1(AntShares.VM.OpCode.JMPIF, null, to, new byte[] { 0, 0 });
                        code.needfix = true;
                        code.srcaddr = src.addr + src.arg1;
                    }
                    break;
                case javaloader.NormalizedByteCode.__if_icmpge:
                    {
                        _Convert1by1(AntShares.VM.OpCode.GTE, null, to);
                        var code = _Convert1by1(AntShares.VM.OpCode.JMPIF, null, to, new byte[] { 0, 0 });
                        code.needfix = true;
                        code.srcaddr = src.addr + src.arg1;
                    }
                    break;

                //    //Stack
                case javaloader.NormalizedByteCode.__dup:
                    _Convert1by1(AntShares.VM.OpCode.DUP, src, to);
                    break;

                //    //Bitwise logic
                //    case CodeEx.And:
                //        _Convert1by1(AntShares.VM.OpCode.AND, src, to);
                //        break;
                //    case CodeEx.Or:
                //        _Convert1by1(AntShares.VM.OpCode.OR, src, to);
                //        break;
                //    case CodeEx.Xor:
                //        _Convert1by1(AntShares.VM.OpCode.XOR, src, to);
                //        break;
                //    case CodeEx.Not:
                //        _Convert1by1(AntShares.VM.OpCode.INVERT, src, to);
                //        break;

                case javaloader.NormalizedByteCode.__iadd:
                case javaloader.NormalizedByteCode.__ladd:
                    _Convert1by1(AntShares.VM.OpCode.ADD, src, to);
                    break;

                case javaloader.NormalizedByteCode.__isub:
                case javaloader.NormalizedByteCode.__lsub:
                    _Convert1by1(AntShares.VM.OpCode.SUB, src, to);
                    break;

                case javaloader.NormalizedByteCode.__imul:
                case javaloader.NormalizedByteCode.__lmul:
                    _Convert1by1(AntShares.VM.OpCode.MUL, src, to);
                    break;
                case javaloader.NormalizedByteCode.__idiv:
                case javaloader.NormalizedByteCode.__ldiv:
                    _Convert1by1(AntShares.VM.OpCode.DIV, src, to);
                    break;

                case javaloader.NormalizedByteCode.__irem:
                case javaloader.NormalizedByteCode.__lrem:
                    _Convert1by1(AntShares.VM.OpCode.MOD, src, to);
                    break;

                //    case CodeEx.Neg:
                //        _Convert1by1(AntShares.VM.OpCode.NEGATE, src, to);
                //        break;
                //    case CodeEx.Shl:
                //        _Convert1by1(AntShares.VM.OpCode.SHL, src, to);
                //        break;
                //    case CodeEx.Shr:
                //    case CodeEx.Shr_Un:
                //        _Convert1by1(AntShares.VM.OpCode.SHR, src, to);
                //        break;

                //    //logic
                //    case CodeEx.Clt:
                //    case CodeEx.Clt_Un:
                //        _Convert1by1(AntShares.VM.OpCode.LT, src, to);
                //        break;
                //    case CodeEx.Cgt:
                //    case CodeEx.Cgt_Un:
                //        _Convert1by1(AntShares.VM.OpCode.GT, src, to);
                //        break;
                //    case CodeEx.Ceq:
                //        _Convert1by1(AntShares.VM.OpCode.NUMEQUAL, src, to);
                //        break;

                //    //call
                //    case CodeEx.Call:
                //    case CodeEx.Callvirt:
                //        _ConvertCall(src, to);
                //        break;

                //    //用上一个参数作为数量，new 一个数组
                //    case CodeEx.Newarr:
                //        skipcount = _ConvertNewArr(method, src, to);
                //        break;


                //    //array
                //    case CodeEx.Ldelem_U1://用意为byte[] 取一部分.....
                //        _ConvertPush(1, src, to);
                //        _Convert1by1(AntShares.VM.OpCode.SUBSTR, null, to);
                //        break;
                //    case CodeEx.Ldelem_Any:
                //    case CodeEx.Ldelem_I:
                //    case CodeEx.Ldelem_I1:
                //    case CodeEx.Ldelem_I2:
                //    case CodeEx.Ldelem_I4:
                //    case CodeEx.Ldelem_I8:
                //    case CodeEx.Ldelem_R4:
                //    case CodeEx.Ldelem_R8:
                //    case CodeEx.Ldelem_Ref:
                //    case CodeEx.Ldelem_U2:
                //    case CodeEx.Ldelem_U4:
                //        _Convert1by1(AntShares.VM.OpCode.PICKITEM, src, to);
                //        break;
                //    case CodeEx.Ldlen:
                //        _Convert1by1(AntShares.VM.OpCode.ARRAYSIZE, src, to);
                //        break;

                //    case CodeEx.Castclass:
                //        break;

                //    case CodeEx.Box:
                //    case CodeEx.Unbox:
                //    case CodeEx.Unbox_Any:
                //    case CodeEx.Break:
                //    //也有可能以后利用这个断点调试
                //    case CodeEx.Conv_I:
                //    case CodeEx.Conv_I1:
                //    case CodeEx.Conv_I2:
                //    case CodeEx.Conv_I4:
                //    case CodeEx.Conv_I8:
                //    case CodeEx.Conv_Ovf_I:
                //    case CodeEx.Conv_Ovf_I_Un:
                //    case CodeEx.Conv_Ovf_I1:
                //    case CodeEx.Conv_Ovf_I1_Un:
                //    case CodeEx.Conv_Ovf_I2:
                //    case CodeEx.Conv_Ovf_I2_Un:
                //    case CodeEx.Conv_Ovf_I4:
                //    case CodeEx.Conv_Ovf_I4_Un:
                //    case CodeEx.Conv_Ovf_I8:
                //    case CodeEx.Conv_Ovf_I8_Un:
                //    case CodeEx.Conv_Ovf_U:
                //    case CodeEx.Conv_Ovf_U_Un:
                //    case CodeEx.Conv_Ovf_U1:
                //    case CodeEx.Conv_Ovf_U1_Un:
                //    case CodeEx.Conv_Ovf_U2:
                //    case CodeEx.Conv_Ovf_U2_Un:
                //    case CodeEx.Conv_Ovf_U4:
                //    case CodeEx.Conv_Ovf_U4_Un:
                //    case CodeEx.Conv_Ovf_U8:
                //    case CodeEx.Conv_Ovf_U8_Un:
                //    case CodeEx.Conv_U:
                //    case CodeEx.Conv_U1:
                //    case CodeEx.Conv_U2:
                //    case CodeEx.Conv_U4:
                //    case CodeEx.Conv_U8:
                //        break;

                //    ///////////////////////////////////////////////
                //    //以下因为支持结构体而出现
                //    //加载一个引用，这里改为加载一个pos值
                //    case CodeEx.Ldloca:
                //    case CodeEx.Ldloca_S:
                //        _ConvertLdLocA(src, to, src.tokenI32);
                //        break;
                //    case CodeEx.Initobj:
                //        _ConvertInitObj(src, to);
                //        break;
                //    case CodeEx.Stfld:
                //        _ConvertStfld(src, to);
                //        break;
                //    case CodeEx.Ldfld:
                //        _ConvertLdfld(src, to);
                //        break;
                default:
                    //throw new Exception("unsupported instruction " + src.code);
                    logger.Log("not support code" + src.code);
                    break;

            }

            return skipcount;
        }

    }
}
