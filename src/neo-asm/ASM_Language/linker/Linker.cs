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
            return null;
        }

        static BuildedFunction BuildFunction(Node.ASMFunction srcfunction)
        {
            BuildedFunction func = new BuildedFunction();
            func.name = srcfunction.Name;
            func.codes = new List<BuildedOpCode>();


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
        static BuildedOpCode BuildOpCode(Node.ASMInstruction inst)
        {
            BuildedOpCode code = new BuildedOpCode();
            if (inst.opcode.isPush)
            {
                code.isJMP = false;
                code.isCALL = false;

                Neo.VM.ScriptBuilder sb = new VM.ScriptBuilder();

                if (inst.valuetext == null)
                    throw new Exception("error push arg");
                if (inst.valuetext[0] == '\'' || inst.valuetext[0] == '"')
                {//string
                    sb.EmitPush(inst.valuetext.Substring(0, inst.valuetext.Length - 2));
                }
                else if (inst.valuetext.ToLower() == "false")
                {
                    sb.EmitPush(false);
                }
                else if (inst.valuetext.ToLower() == "true")
                {
                    sb.EmitPush(false);
                }
                else if (inst.valuetext[0] == '[')
                {//bytes
                    var bts = inst.valuetext.Substring(1, inst.valuetext.Length - 2).Split(',');
                    var bytes = new byte[bts.Length];
                    for (var i = 0; i < bts.Length; i++)
                    {
                        var bs = bts[i];
                        if (string.IsNullOrWhiteSpace(bs))
                            bytes[i] = 0;
                        else if (bs.IndexOf("0x") == 0)
                        {
                            bytes[i] = byte.Parse(bs.Substring(2),System.Globalization.NumberStyles.HexNumber);
                        }
                        else
                        {
                            bytes[i] = byte.Parse(bs);
                        }
                    }
                    sb.EmitPush(bytes);
                }

                else
                {//number
                    var num = System.Numerics.BigInteger.Parse(inst.valuetext);
                    sb.EmitPush(num);
                }
                code.finalbytes = sb.ToArray();

            }
            else if (inst.opcode.opcodeVM == VM.OpCode.JMP || inst.opcode.opcodeVM == VM.OpCode.JMPIF || inst.opcode.opcodeVM == VM.OpCode.JMPIFNOT)
            {
                //2 bytes data

                code.isJMP = true;
                code.isCALL = false;
                code.finalbytes = new byte[3];
                code.finalbytes[0] = (byte)inst.opcode.opcodeVM;
            }
            else if (inst.opcode.opcodeVM == VM.OpCode.CALL)
            {
                //2 bytes data
                code.isJMP = false;
                code.isCALL = true;
                code.finalbytes = new byte[3];
                code.finalbytes[0] = (byte)inst.opcode.opcodeVM;

            }
            else if (inst.opcode.opcodeVM == VM.OpCode.SYSCALL)
            {
                //4字节参数
                code.isJMP = false;
                code.isCALL = false;
                code.finalbytes = new byte[5];
                code.finalbytes[0] = (byte)inst.opcode.opcodeVM;

                uint api = 0;
                if (!uint.TryParse(inst.valuetext, out api))
                {
                    if(inst.valuetext[0]=='"')
                    {
                        api = ToInteropMethodHash(inst.valuetext.Substring(0, inst.valuetext.Length - 2));
                    }
                    else
                    {
                        throw new Exception("unknown api format");
                    }
                }
                var data = BitConverter.GetBytes(api);
                Array.Copy(data, 0, code.finalbytes, 1, 4);
            }
            else
            {
                //其它无参数
                code.finalbytes = new byte[1];
                code.finalbytes[0] = (byte)inst.opcode.opcodeVM;
                code.isCALL = false;
                code.isJMP = false;
            }
            return code;
        }
    }
}
