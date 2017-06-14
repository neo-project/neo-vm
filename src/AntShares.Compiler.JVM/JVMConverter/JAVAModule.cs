using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static javaloader.ClassFile;
using static javaloader.ClassFile.Method;

namespace AntShares.Compiler.JVM
{
    public class JavaClass
    {
        public JavaClass(javaloader.ClassFile classfile, string[] srcfile = null)
        {
            this.classfile = classfile;
            this.srcfile = srcfile;
            if (this.srcfile == null)
                this.srcfile = new string[0];
            foreach (var f in this.classfile.Fields)
            {
                this.fields.Add(f.Name, f.Signature);
            }
            foreach (var m in this.classfile.Methods)
            {
                var nm = new JavaMethod(this, m);
                this.methods.Add(m.Name, nm);
            }
            this.superClass = this.classfile.SuperClass;
        }
        public string[] srcfile;
        public string superClass;
        public javaloader.ClassFile classfile;
        public Dictionary<string, string> fields = new Dictionary<string, string>();
        public Dictionary<string, JavaMethod> methods = new Dictionary<string, JavaMethod>();

    }
    public class JavaMethod
    {
        public JavaClass DeclaringType;
        public javaloader.ClassFile.Method method;
        public string returnType;
        public List<string> paramTypes = new List<string>();
        public Dictionary<int, OpCode> body_Codes = new Dictionary<int, OpCode>();
        public List<AntsParam> body_Variables = new List<AntsParam>();

        public int MaxVariableIndex = 0;
        //public int addLocal_VariablesCount = 0;
        //不做表转换了，直接按最大索引给
        public Dictionary<int, int> argTable;// new List<int>();//index->arg index
        //public Dictionary<int, int> localTable;//index->localIndex;

        public JavaMethod(JavaClass type, javaloader.ClassFile.Method method)
        {
            this.DeclaringType = type;
            this.method = method;
            //method.LocalVariableTableAttribute
            this.argTable = new Dictionary<int, int>();
            //this.localTable = new Dictionary<int, int>();
            for (var i = 0; i < method.ArgMap.Length; i++)
            {
                var ind = method.ArgMap[i];
                if (ind >= 0)
                    this.argTable[ind] = i;
            }
            scanTypes(method.Signature);
            Dictionary<int, string> local = new Dictionary<int, string>();

            foreach (var lv in this.method.LocalVariableTableAttribute)
            {
                var ind = lv.index;
                if (this.argTable.ContainsValue(ind) == false)
                {

                    var desc = lv.name + ";" + lv.descriptor;
                    if (local.ContainsKey(ind))
                    {
                        local[ind] = local[ind] + "||" + desc;
                    }
                    else
                    {
                        local[ind] = desc;
                    }
                }
                this.MaxVariableIndex = Math.Max(ind + 1, this.MaxVariableIndex);
            }
            //for (var i = 0; i < local.Count; i++)
            //{
            //    this.localTable[local.Keys.ToArray()[i]] = i;
            //}

            {
                this.body_Variables = new List<AntsParam>();

                //var addLocal_VariablesCount = this.method.MaxLocals - this.paramTypes.Count;
                //if (addLocal_VariablesCount < local.Count)
                //{
                //    throw new Exception("not impossible.");
                //}
                //for (var i = 0; i < addLocal_VariablesCount; i++)
                //{
                //    this.body_Variables.Add(new Param("_noname", ""));
                //}

                for (var i = 0; i < local.Count; i++)
                {
                    this.body_Variables.Add(new AntsParam("_noname", ""));
                }
                foreach (var lv in local)
                {
                    this.body_Variables[lv.Key - this.paramTypes.Count] = new AntsParam("local", lv.Value);
                }
            }

            for (var i = 0; i < this.method.Instructions.Length; i++)
            {
                Instruction code = this.method.Instructions[i];
                var opcode = new OpCode();

                opcode.InitToken(this, code);
                this.body_Codes[code.PC] = opcode;
            }
            // this.method.LocalVariableTableAttribute

        }
        string getTypeString(string sign, ref int i)
        {
            if (sign[i] == '[') //for array
            {
                i++;
                return "[" + getTypeString(sign, ref i);
            }
            else if (sign[i] == 'V')
            {
                return "void";
            }
            else if (sign[i] == 'I') //a int
            {
                return "int";
            }
            else if (sign[i] == 'J') //a long
            {
                return "long";
            }
            else if (sign[i] == 'B')
            {
                return "byte";
            }
            else if (sign[i] == 'S')
            {
                return "short";
            }
            else if (sign[i] == 'Z')
            {
                return "boolean";
            }
            else if (sign[i] == 'F')
            {
                return "float";
            }
            else if (sign[i] == 'D')
            {
                return "double";
            }
            else if (sign[i] == 'L')//a long string
            {
                var i2 = sign.IndexOf(';');

                var type = sign.Substring(i + 1, i2 - i - 1);

                i = i2;
                return type;
            }
            else
            {
                throw new Exception("not parsed sign.");
            }
        }
        void scanTypes(string sign)
        {
            bool forreturn = false;
            for (var i = 0; i < sign.Length; i++)
            {

                if (sign[i] == '(') //beginparam
                {
                    continue;
                }
                else if (sign[i] == ')')//endparam
                {
                    forreturn = true;
                    continue;
                }
                else
                {
                    string type = getTypeString(sign, ref i);
                    if (forreturn)
                    {
                        returnType = type;
                        return;
                    }
                    else
                    {
                        paramTypes.Add(type);
                        continue;
                    }

                }
            }
        }

        public int GetNextCodeAddr(int srcaddr)
        {
            bool bskip = false;
            foreach (var key in this.body_Codes.Keys)
            {
                if (key == srcaddr)
                {
                    bskip = true;
                    continue;
                }
                if (bskip)
                {
                    return key;
                }

            }
            return -1;
        }
    }

    public class OpCode
    {
        public javaloader.NormalizedByteCode code;
        public override string ToString()
        {
            var info = "IL_" + addr.ToString("X04") + " " + code + " ";
            if (this.tokenValueType == TokenValueType.Method)
                info += tokenMethod;
            if (this.tokenValueType == TokenValueType.String)
                info += tokenStr;

            if (debugline >= 0)
            {
                info += "(" + debugline + ")";
            }
            return info;
        }
        public enum TokenValueType
        {
            Nothing,
            Addr,//地址
            AddrArray,
            String,
            Type,
            Field,
            Method,
            I32,
            I64,
            OTher,
        }
        public TokenValueType tokenValueType = TokenValueType.Nothing;
        public int addr;
        public int debugline = -1;
        public string debugcode;
        public int arg1;
        public int arg2;

        public object tokenUnknown;
        public int tokenAddr_Index;
        //public int tokenAddr;
        public int[] tokenAddr_Switch;
        public string tokenType;
        public string tokenField;
        public string tokenMethod;
        public int tokenI32;
        public Int64 tokenI64;
        public float tokenR32;
        public double tokenR64;
        public string tokenStr;
        public void InitToken(JavaMethod method, Instruction ins)
        {
            this.code = ins.NormalizedOpCode;
            this.arg1 = ins.Arg1;
            this.arg2 = ins.Arg2;
            this.addr = ins.PC;
            if (method.method.LineNumberTableAttribute.TryGetValue(this.addr, out this.debugline) == false)
            {
                this.debugline = -1;
            }
            if (this.debugline >= 0)
            {
                if (this.debugline - 1 < method.DeclaringType.srcfile.Length)
                    this.debugcode = method.DeclaringType.srcfile[this.debugline - 1];
            }
            switch (code)
            {
                case javaloader.NormalizedByteCode.__iconst:
                    this.tokenI32 = this.arg1;
                    break;
                //case javaloader.NormalizedByteCode.__newarray:
                //    var c = method.DeclaringType.classfile.constantpool[this.arg1];
                //    break;
                case javaloader.NormalizedByteCode.__astore:
                    break;
                default:
                    this.tokenUnknown = ins;
                    this.tokenValueType = TokenValueType.Nothing;
                    break;
            }
        }

    }
}
