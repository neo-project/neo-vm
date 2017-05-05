using System;
using System.Collections.Generic;

namespace AntShares.Compiler.MSIL
{

    public class Converter
    {
        public static byte[] Convert(System.IO.Stream dllstream, ILogger logger = null)
        {
            var module = new ILModule();
            module.LoadModule(dllstream, null);
            if (logger == null)
            {
                logger = new DefLogger();
            }
            var converter = new ModuleConverter(logger);
            //有异常的话在 convert 函数中会直接throw 出来
            var antmodule = converter.Convert(module);
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
    /// <summary>
    /// 从ILCode 向小蚁 VM 转换的转换器
    /// </summary>
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
        public AntsModule outModule;
        public Dictionary<ILMethod, AntsMethod> methodLink = new Dictionary<ILMethod, AntsMethod>();
        public AntsModule Convert(ILModule _in)
        {
            logger.Log("beginConvert.");
            this.outModule = new AntsModule(this.logger);
            foreach (var t in _in.mapType)
            {
                if (t.Key[0] == '<') continue;//系统的，不要
                if (t.Key.Contains("_API_")) continue;//api的，不要
                foreach (var m in t.Value.methods)
                {
                    if (m.Value.method == null) continue;
                    AntsMethod nm = new AntsMethod();
                    nm.name = m.Value.method.FullName;
                    this.methodLink[m.Value] = nm;
                    outModule.mapMethods[nm.name] = nm;


                }
            }
            foreach (var t in _in.mapType)
            {
                if (t.Key[0] == '<') continue;//系统的，不要
                if (t.Key.Contains("_API_")) continue;//api的，不要
                foreach (var m in t.Value.methods)
                {
                    if (m.Value.method == null) continue;
                    var nm = this.methodLink[m.Value];
                    try
                    {
                        this.ConvertMethod(m.Value, nm);
                    }
                    catch (Exception err)
                    {
                        logger.Log("error:" + err.Message);
                    }
                }
            }
            //转换完了，做个link，全部拼到一起
            string mainmethod = "";
            foreach (var key in outModule.mapMethods.Keys)
            {
                if (key.Contains("Verify"))
                {
                    mainmethod = key;
                }
            }
            if(mainmethod=="")
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
        private void LinkCode(string main)
        {
            if(this.outModule.mapMethods.ContainsKey(main)==false)
            {
                throw new Exception("找不到名为" + main + "的入口");
                return;
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
        private void ConvertMethod(ILMethod from, AntsMethod to)
        {
            to.returntype = from.returntype;
            foreach (var src in from.paramtypes)
            {
                to.paramtypes.Add(new ILParam(src.name, src.type));
            }


            this.addr = 0;
            this.addrconv.Clear();

            //插入一个记录深度的代码，再往前的是参数
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
                    if (src.code == CodeEx.Ret)//before return 
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

        //Dictionary<string, string[]> srccodes = new Dictionary<string, string[]>();
        //string getSrcCode(string url, int line)
        //{
        //    if (url == null || url == "") return "";
        //    if (srccodes.ContainsKey(url) == false)
        //    {
        //        srccodes[url] = System.IO.File.ReadAllLines(url);
        //    }
        //    if (srccodes.ContainsKey(url) != false)
        //    {
        //        var file = srccodes[url];
        //        if (line > 0 && line <= file.Length)
        //        {
        //            return file[line - 1];
        //        }
        //    }
        //    return "";
        //}
        static byte[] str2Pushdata1bytes(string str)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(str);
            if (bytes.Length > 252) throw new Exception("string is to long");
            return toPushdata1bytes(bytes);
        }
        static byte[] int2Pushdata1bytes(int di)
        {
            var b = BitConverter.GetBytes(di);
            return toPushdata1bytes(b);
        }
        static byte[] toPushdata1bytes(byte[] data)
        {
            var bytes = new byte[data.Length + 1];
            bytes[0] = (byte)data.Length;
            for (var i = 0; i < data.Length; i++)
            {
                bytes[i + 1] = data[i];
            }
            return bytes;

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

        private int ConvertCode(ILMethod method, OpCode src, AntsMethod to)
        {
            int skipcount = 0;
            switch (src.code)
            {
                case CodeEx.Nop:
                    _Convert1by1(AntShares.VM.OpCode.NOP, src, to);
                    break;
                case CodeEx.Ret:
                    //return 在外面特殊处理了
                    _Insert1(AntShares.VM.OpCode.RET, null, to);
                    break;
                case CodeEx.Pop:
                    _Convert1by1(AntShares.VM.OpCode.DROP, src, to);
                    break;

                case CodeEx.Ldc_I4_0:
                    _Convert1by1(AntShares.VM.OpCode.PUSHDATA1, src, to, int2Pushdata1bytes(0));
                    break;
                case CodeEx.Ldc_I4_1:
                    _Convert1by1(AntShares.VM.OpCode.PUSHDATA1, src, to, int2Pushdata1bytes(1));
                    break;
                case CodeEx.Ldc_I4_2:
                    _Convert1by1(AntShares.VM.OpCode.PUSHDATA1, src, to, int2Pushdata1bytes(2));
                    break;
                case CodeEx.Ldc_I4_3:
                    _Convert1by1(AntShares.VM.OpCode.PUSHDATA1, src, to, int2Pushdata1bytes(3));
                    break;
                case CodeEx.Ldc_I4_4:
                    _Convert1by1(AntShares.VM.OpCode.PUSHDATA1, src, to, int2Pushdata1bytes(4));
                    break;
                case CodeEx.Ldc_I4_5:
                    _Convert1by1(AntShares.VM.OpCode.PUSHDATA1, src, to, int2Pushdata1bytes(5));
                    break;
                case CodeEx.Ldc_I4_6:
                    _Convert1by1(AntShares.VM.OpCode.PUSHDATA1, src, to, int2Pushdata1bytes(6));
                    break;
                case CodeEx.Ldc_I4_7:
                    _Convert1by1(AntShares.VM.OpCode.PUSHDATA1, src, to, int2Pushdata1bytes(7));
                    break;
                case CodeEx.Ldc_I4_8:
                    _Convert1by1(AntShares.VM.OpCode.PUSHDATA1, src, to, int2Pushdata1bytes(1));
                    break;
                case CodeEx.Ldc_I4_S:
                    _Convert1by1(AntShares.VM.OpCode.PUSHDATA1, src, to, int2Pushdata1bytes(src.tokenI32));
                    break;

                case CodeEx.Stloc_0:
                    _ConvertStLoc(src, to, 0);
                    break;
                case CodeEx.Stloc_1:
                    _ConvertStLoc(src, to, 1);
                    break;
                case CodeEx.Stloc_2:
                    _ConvertStLoc(src, to, 2);
                    break;
                case CodeEx.Stloc_3:
                    _ConvertStLoc(src, to, 3);
                    break;
                case CodeEx.Stloc_S:
                    _ConvertStLoc(src, to, src.tokenI32);
                    break;

                case CodeEx.Ldloc_0:
                    _ConvertLdLoc(src, to, 0);
                    break;
                case CodeEx.Ldloc_1:
                    _ConvertLdLoc(src, to, 1);
                    break;
                case CodeEx.Ldloc_2:
                    _ConvertLdLoc(src, to, 2);
                    break;
                case CodeEx.Ldloc_3:
                    _ConvertLdLoc(src, to, 3);
                    break;
                case CodeEx.Ldloc_S:
                    _ConvertLdLoc(src, to, src.tokenI32);
                    break;

                case CodeEx.Ldarg_0:
                    _ConvertLdArg(src, to, 0);
                    break;
                case CodeEx.Ldarg_1:
                    _ConvertLdArg(src, to, 1);
                    break;
                case CodeEx.Ldarg_2:
                    _ConvertLdArg(src, to, 2);
                    break;
                case CodeEx.Ldarg_3:
                    _ConvertLdArg(src, to, 3);
                    break;
                case CodeEx.Ldarg_S:
                case CodeEx.Ldarg:
                    _ConvertLdArg(src, to, src.tokenI32);
                    break;
                //需要地址轉換的情況
                case CodeEx.Br_S:
                case CodeEx.Br:
                    {
                        var code = _Convert1by1(AntShares.VM.OpCode.JMP, src, to, new byte[] { 0, 0 });
                        code.needfix = true;
                        code.srcaddr = src.tokenAddr_Index;
                    }

                    break;
                case CodeEx.Brtrue:
                case CodeEx.Brtrue_S:
                    {
                        var code = _Convert1by1(AntShares.VM.OpCode.JMPIF, src, to, new byte[] { 0, 0 });
                        code.needfix = true;
                        code.srcaddr = src.tokenAddr_Index;
                    }
                    break;
                case CodeEx.Brfalse:
                case CodeEx.Brfalse_S:
                    {
                        var code = _Convert1by1(AntShares.VM.OpCode.JMPIFNOT, src, to, new byte[] { 0, 0 });
                        code.needfix = true;
                        code.srcaddr = src.tokenAddr_Index;
                    }
                    break;
                case CodeEx.Blt:
                case CodeEx.Blt_S:
                    {
                        _Convert1by1(AntShares.VM.OpCode.LT, src, to);
                        var code = _Convert1by1(AntShares.VM.OpCode.JMPIF, null, to, new byte[] { 0, 0 });
                        code.needfix = true;
                        code.srcaddr = src.tokenAddr_Index;
                    }
                    break;
                case CodeEx.Ble:
                case CodeEx.Ble_S:
                    {
                        _Convert1by1(AntShares.VM.OpCode.LTE, src, to);
                        var code = _Convert1by1(AntShares.VM.OpCode.JMPIF, null, to, new byte[] { 0, 0 });
                        code.needfix = true;
                        code.srcaddr = src.tokenAddr_Index;
                    }
                    break;
                case CodeEx.Bgt:
                case CodeEx.Bgt_S:
                    {
                        _Convert1by1(AntShares.VM.OpCode.GT, src, to);
                        var code = _Convert1by1(AntShares.VM.OpCode.JMPIF, null, to, new byte[] { 0, 0 });
                        code.needfix = true;
                        code.srcaddr = src.tokenAddr_Index;
                    }
                    break;
                case CodeEx.Bge:
                case CodeEx.Bge_S:
                    {
                        _Convert1by1(AntShares.VM.OpCode.GTE, src, to);
                        var code = _Convert1by1(AntShares.VM.OpCode.JMPIF, null, to, new byte[] { 0, 0 });
                        code.needfix = true;
                        code.srcaddr = src.tokenAddr_Index;
                    }
                    break;
                //math
                case CodeEx.Add:
                case CodeEx.Add_Ovf:
                case CodeEx.Add_Ovf_Un:
                    _Convert1by1(AntShares.VM.OpCode.ADD, src, to);
                    break;
                case CodeEx.Sub:
                case CodeEx.Sub_Ovf:
                case CodeEx.Sub_Ovf_Un:
                    _Convert1by1(AntShares.VM.OpCode.SUB, src, to);
                    break;
                case CodeEx.Mul:
                case CodeEx.Mul_Ovf:
                case CodeEx.Mul_Ovf_Un:
                    _Convert1by1(AntShares.VM.OpCode.MUL, src, to);
                    break;
                case CodeEx.Div:
                case CodeEx.Div_Un:
                    _Convert1by1(AntShares.VM.OpCode.DIV, src, to);
                    break;
                case CodeEx.Rem:
                case CodeEx.Rem_Un:
                    _Convert1by1(AntShares.VM.OpCode.MOD, src, to);
                    break;

                //logic
                case CodeEx.Clt:
                case CodeEx.Clt_Un:
                    _Convert1by1(AntShares.VM.OpCode.LT, src, to);
                    break;
                case CodeEx.Cgt:
                case CodeEx.Cgt_Un:
                    _Convert1by1(AntShares.VM.OpCode.GT, src, to);
                    break;
                case CodeEx.Ceq:
                    _Convert1by1(AntShares.VM.OpCode.NUMEQUAL, src, to);
                    break;

                //call
                case CodeEx.Call:
                case CodeEx.Callvirt:
                    _ConvertCall(src, to);
                    break;

                //用上一个参数作为数量，new 一个数组
                case CodeEx.Newarr:
                    skipcount = _ConvertNewArr(method, src, to);
                    break;


                //array
                case CodeEx.Ldelem_Ref:
                    _Convert1by1(AntShares.VM.OpCode.PICKITEM, src, to);
                    break;
                case CodeEx.Ldlen:
                    _Convert1by1(AntShares.VM.OpCode.ARRAYSIZE, src, to);
                    break;

                case CodeEx.Castclass:
                    break;

                case CodeEx.Box:
                case CodeEx.Unbox:
                case CodeEx.Conv_I:
                case CodeEx.Conv_I1:
                case CodeEx.Conv_I2:
                case CodeEx.Conv_I4:
                case CodeEx.Conv_I8:
                case CodeEx.Conv_U:
                case CodeEx.Conv_U1:
                case CodeEx.Conv_U2:
                case CodeEx.Conv_U4:
                case CodeEx.Conv_U8:
                    break;

                default:
                    logger.Log("not support code" + src.code);
                    break;

            }

            return skipcount;
        }

    }
}
