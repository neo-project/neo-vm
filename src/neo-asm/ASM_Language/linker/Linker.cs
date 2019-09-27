using Neo.VM;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Neo.ASML.Linker
{
    public class Linker
    {
        public static BuildedModule CreateModule(Neo.ASML.Node.ASMProject project)
        {
            BuildedModule module = new BuildedModule();
            module.methods = new Dictionary<string, BuildedFunction>();
            foreach (var node in project.nodes)
            {
                if (node is ASML.Node.ASMFunction)
                {
                    var func = node as Node.ASMFunction;
                    var buildedfunc = BuildFunction(func);
                    module.methods.Add(buildedfunc.name, buildedfunc);
                }
            }
            return module;
        }


        public static byte[] Link(BuildedModule module, string entrypoint = "Main")
        {
            module.buildmethods = new List<string>();
            var entrymethod = module.methods[entrypoint];
            TouchFunction(module, entrymethod);

            //convert call addr
            //convert jmp addr
            foreach (var m in module.buildmethods)
            {
                var method = module.methods[m];
                foreach (var c in method.codes)
                {
                    if (c.CALLTarget != null)
                    {
                        var jmppos = module.methods[c.CALLTarget].addr;
                        var curaddr = method.addr + c.addr;

                        Int16 addroff = (Int16)(jmppos - curaddr);

                        var bs = BitConverter.GetBytes(addroff);
                        c.finalbytes[1] = bs[0];
                        c.finalbytes[2] = bs[1];
                    }
                }
            }

            return module.getFinalBytes();
        }

        static BuildedFunction BuildFunction(Node.ASMFunction srcfunction)
        {
            BuildedFunction func = new BuildedFunction();
            func.name = srcfunction.Name;
            func.codes = new List<BuildedOpCode>();

            //build func
            Dictionary<string, BuildedOpCode> label2code = new Dictionary<string, BuildedOpCode>();
            List<string> labels = new List<string>();
            var addr = 0;
            foreach (var node in srcfunction.nodes)
            {
                if (node is ASML.Node.ASMLabel)
                {
                    var label = node as ASML.Node.ASMLabel;
                    labels.Add(label.label);
                }
                else if (node is ASML.Node.ASMInstruction)
                {
                    var inst = node as ASML.Node.ASMInstruction;

                    var opcode = BuildOpCode(inst);

                    //fill source
                    opcode.srcInstruction = inst;
                    //fill addr
                    opcode.addr = addr;
                    addr += opcode.finalbytes.Length;
                    //fill label
                    if (labels.Count > 0)
                    {
                        opcode.labels = labels.ToArray();
                        foreach (var l in opcode.labels)
                        {
                            label2code.Add(l, opcode);
                        }
                        labels.Clear();
                    }
                    else
                        opcode.labels = null;
                    func.codes.Add(opcode);
                }
            }
            //convert jmp addr
            foreach (var c in func.codes)
            {
                if (c.JMPTarget != null)
                {
                    var jmppos = label2code[c.JMPTarget].addr;
                    var curaddr = c.addr;
                    Int16 addroff = (Int16)(jmppos - curaddr);
                    var bs = BitConverter.GetBytes(addroff);
                    c.finalbytes[1] = bs[0];
                    c.finalbytes[2] = bs[1];
                }
            }

            return func;
        }
        static uint ToInteropMethodHash(string method)
        {
            return ToInteropMethodHash(Encoding.ASCII.GetBytes(method));
        }
        [ThreadStatic]
        static SHA256 sha256;
        static uint ToInteropMethodHash(byte[] method)
        {
            if (sha256 == null)
            {
                sha256 = SHA256.Create();
            }
            return BitConverter.ToUInt32(sha256.ComputeHash(method), 0);
        }

        static System.Numerics.BigInteger ParseNumber(string str)
        {
            if (str.ToLower() == "true")
                return 1;
            else if (str.ToLower() == "false")
                return 0;
            else if (str.IndexOf("0x") == 0)
            {
                return System.Numerics.BigInteger.Parse(str.Substring(2), System.Globalization.NumberStyles.HexNumber);
            }
            else
            {
                return System.Numerics.BigInteger.Parse(str);
            }
        }
        static byte[] ParseData(string str)
        {
            if (str[0] == '[')
            {//bytes
                var bts = str.Substring(1, str.Length - 2).Split(',');
                var bytes = new byte[bts.Length];
                for (var i = 0; i < bts.Length; i++)
                {
                    var bs = bts[i];
                    if (string.IsNullOrWhiteSpace(bs))
                        bytes[i] = 0;
                    else if (bs.IndexOf("0x") == 0)
                    {
                        bytes[i] = byte.Parse(bs.Substring(2), System.Globalization.NumberStyles.HexNumber);
                    }
                    else
                    {
                        bytes[i] = byte.Parse(bs);
                    }
                }
                return bytes;
            }
            else if (str[0] == '\'' || str[0] == '"')
            {//string
                var strvalue = str.Substring(0, str.Length - 2);
                return Encoding.UTF8.GetBytes(strvalue);
            }
            else
            {
                var num = ParseNumber(str);
                return num.ToByteArray();
            }
        }
        static BuildedOpCode BuildOpCode(Node.ASMInstruction inst)
        {
            BuildedOpCode code = new BuildedOpCode();
            if (inst.opcode.isPush)
            {
                code.JMPTarget = null;
                code.CALLTarget = null;

                Neo.VM.ScriptBuilder sb = new VM.ScriptBuilder();

                if (inst.valuetext == null)
                    throw new Exception("error push arg");
                if (inst.valuetext[0] == '\'' || inst.valuetext[0] == '"' || inst.valuetext[0] == '[')
                {//string or bytearray
                    byte[] data = ParseData(inst.valuetext);
                    sb.EmitPush(data);
                }
                else
                {//number
                    var num = ParseNumber(inst.valuetext);
                    sb.EmitPush(num);
                }
                code.finalbytes = sb.ToArray();

            }
            else if (inst.opcode.opcodeVM == VM.OpCode.JMP || inst.opcode.opcodeVM == VM.OpCode.JMPIF || inst.opcode.opcodeVM == VM.OpCode.JMPIFNOT)
            {
                //2 bytes data

                code.JMPTarget = inst.valuetext;
                if (inst.valuetext[0] == '"' || inst.valuetext[0] == '\'')
                {
                    code.JMPTarget = inst.valuetext.Substring(1, inst.valuetext.Length - 2);
                }

                code.CALLTarget = null;
                code.finalbytes = new byte[3];
                code.finalbytes[0] = (byte)inst.opcode.opcodeVM;
            }
            else if (inst.opcode.opcodeVM == VM.OpCode.CALL)
            {
                //2 bytes data
                code.JMPTarget = null;
                code.CALLTarget = inst.valuetext;
                if (inst.valuetext[0] == '"' || inst.valuetext[0] == '\'')
                {
                    code.CALLTarget = inst.valuetext.Substring(1, inst.valuetext.Length - 2);
                }
                code.finalbytes = new byte[3];
                code.finalbytes[0] = (byte)inst.opcode.opcodeVM;

            }
            else if (inst.opcode.opcodeVM == VM.OpCode.SYSCALL)
            {
                //4字节参数
                code.JMPTarget = null;
                code.CALLTarget = null;
                code.finalbytes = new byte[5];
                code.finalbytes[0] = (byte)inst.opcode.opcodeVM;


                if (inst.valuetext[0] == '[')
                {// a data value
                    var data = ParseData(inst.valuetext);
                    Array.Copy(data, 0, code.finalbytes, 1, 4);
                }
                else if (inst.valuetext[0] == '"')
                {// a string value
                    uint api = ToInteropMethodHash(inst.valuetext.Substring(0, inst.valuetext.Length - 2));
                    var data = BitConverter.GetBytes(api);
                    Array.Copy(data, 0, code.finalbytes, 1, 4);
                }
                else
                {// a number value
                    uint api = (uint)ParseNumber(inst.valuetext);
                    var data = BitConverter.GetBytes(api);
                    Array.Copy(data, 0, code.finalbytes, 1, 4);
                }
            }
            else
            {
                //其它无参数
                code.finalbytes = new byte[1];
                code.finalbytes[0] = (byte)inst.opcode.opcodeVM;
                code.JMPTarget = null;
                code.CALLTarget = null;
            }
            return code;
        }

        static void TouchFunction(BuildedModule module, BuildedFunction func)
        {
            if (module.buildmethods.Count > 0)
            {
                var m = module.buildmethods[module.buildmethods.Count - 1];
                var lastfunc = module.methods[m];
                func.addr = lastfunc.addr + lastfunc.getFinalLength();
            }
            else
            {
                func.addr = 0;
            }
            module.buildmethods.Add(func.name);


            foreach (var c in func.codes)
            {
                if (c.CALLTarget != null)
                {
                    if (module.methods.ContainsKey(c.CALLTarget) == false)
                        throw new Exception("call method is not exist:" + c.CALLTarget);
                    var touchfunc = module.methods[c.CALLTarget];

                    if (module.buildmethods.Contains(c.CALLTarget) == false)
                    {
                        TouchFunction(module, touchfunc);
                    }
                }
            }
        }
    }
}
