using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// 这个文件负责 IL dll的解析，核心解析是使用mono.cecil,这里进行了一些预先整理
/// 使用时更便捷
/// </summary>
namespace AntShares.Compiler.MSIL
{
    public class ILModule
    {
        public Mono.Cecil.ModuleDefinition module = null;
        public List<string> moduleref = new List<string>();
        public Dictionary<string, ILType> mapType = new Dictionary<string, ILType>();
        public ILModule()
        {

        }
        public void LoadModule(System.IO.Stream dllStream, System.IO.Stream pdbStream)
        {
            this.module = Mono.Cecil.ModuleDefinition.ReadModule(dllStream);
#if WITHPDB
            if (pdbStream != null)
            {
                var debugInfoLoader = new Mono.Cecil.Pdb.PdbReaderProvider();

                module.ReadSymbols(debugInfoLoader.GetSymbolReader(module, pdbStream));
            }
#endif
            if (module.HasAssemblyReferences)
            {
                foreach (var ar in module.AssemblyReferences)
                {
                    if (moduleref.Contains(ar.Name) == false)
                        moduleref.Add(ar.Name);
                    if (moduleref.Contains(ar.FullName) == false)
                        moduleref.Add(ar.FullName);
                }
            }
            //mapModule[module.Name] = module;
            if (module.HasTypes)
            {
                foreach (var t in module.Types)
                {

                    mapType[t.FullName] = new ILType(this, t);
                    if(t.HasNestedTypes)
                    {
                        foreach(var nt in t.NestedTypes)
                        {
                            mapType[nt.FullName] = new ILType(this, nt);

                        }
                    }
                   
                }
            }
        }

    }
    public class ILType
    {
        Mono.Cecil.TypeDefinition type;
        public Dictionary<string, string> fields = new Dictionary<string, string>();
        public Dictionary<string, ILMethod> methods = new Dictionary<string, ILMethod>();
        public ILType(ILModule module, Mono.Cecil.TypeDefinition type)
        {
            this.type = type;
            foreach (Mono.Cecil.FieldDefinition f in type.Fields)
            {
                this.fields.Add(f.Name, f.FieldType.FullName);
            }
            foreach (Mono.Cecil.MethodDefinition m in type.Methods)
            {
                if (m.IsStatic == false)
                {
                    var method = new ILMethod(this, null);
                    method.fail = "只能导出static 函数";
                    methods[m.Name] = method;
                }
                else
                {
                    var method = new ILMethod(this, m);
                    methods[m.Name] = method;
                }
            }
        }

    }
    public class ILMethod
    {
        public ILMethod(ILType type, Mono.Cecil.MethodDefinition method)
        {
            this.method = method;
            if (method != null)
            {
                returntype = method.ReturnType.FullName;
                if (method.HasParameters)
                {
                    hasParam = true;
                    foreach (var p in method.Parameters)
                    {
                        this.paramtypes.Add(new AntsParam(p.Name, p.ParameterType.FullName));
                    }
                }
                if (method.HasBody)
                {
                    var bodyNative = method.Body;
                    if (bodyNative.HasVariables)
                    {
                        foreach (var v in bodyNative.Variables)
                        {
                            this.body_Variables.Add(new AntsParam(v.Name, v.VariableType.FullName));
                        }
                    }
                    for (int i = 0; i < bodyNative.Instructions.Count; i++)
                    {
                        var code = bodyNative.Instructions[i];
                        OpCode c = new OpCode();
                        c.code = (CodeEx)(int)code.OpCode.Code;
                        c.addr = code.Offset;
                        if (code.SequencePoint != null)
                        {
                            c.debugcode = code.SequencePoint.Document.Url;
                            c.debugline = code.SequencePoint.StartLine;
                        }
                        c.InitToken(code.Operand);
                        this.body_Codes.Add(c.addr, c);
                    }
                }
            }
        }

        public string returntype;
        public List<AntsParam> paramtypes = new List<AntsParam>();
        public bool hasParam = false;
        public Mono.Cecil.MethodDefinition method;
        public List<AntsParam> body_Variables = new List<AntsParam>();
        public SortedDictionary<int, OpCode> body_Codes = new SortedDictionary<int, OpCode>();
        public string fail = null;
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

    public enum CodeEx
    {
        Nop,
        Break,
        Ldarg_0,
        Ldarg_1,
        Ldarg_2,
        Ldarg_3,
        Ldloc_0,
        Ldloc_1,
        Ldloc_2,
        Ldloc_3,
        Stloc_0,
        Stloc_1,
        Stloc_2,
        Stloc_3,
        Ldarg_S,
        Ldarga_S,
        Starg_S,
        Ldloc_S,
        Ldloca_S,
        Stloc_S,
        Ldnull,
        Ldc_I4_M1,
        Ldc_I4_0,
        Ldc_I4_1,
        Ldc_I4_2,
        Ldc_I4_3,
        Ldc_I4_4,
        Ldc_I4_5,
        Ldc_I4_6,
        Ldc_I4_7,
        Ldc_I4_8,
        Ldc_I4_S,
        Ldc_I4,
        Ldc_I8,
        Ldc_R4,
        Ldc_R8,
        Dup,
        Pop,
        Jmp,
        Call,
        Calli,
        Ret,
        Br_S,
        Brfalse_S,
        Brtrue_S,
        Beq_S,
        Bge_S,
        Bgt_S,
        Ble_S,
        Blt_S,
        Bne_Un_S,
        Bge_Un_S,
        Bgt_Un_S,
        Ble_Un_S,
        Blt_Un_S,
        Br,
        Brfalse,
        Brtrue,
        Beq,
        Bge,
        Bgt,
        Ble,
        Blt,
        Bne_Un,
        Bge_Un,
        Bgt_Un,
        Ble_Un,
        Blt_Un,
        Switch,
        Ldind_I1,
        Ldind_U1,
        Ldind_I2,
        Ldind_U2,
        Ldind_I4,
        Ldind_U4,
        Ldind_I8,
        Ldind_I,
        Ldind_R4,
        Ldind_R8,
        Ldind_Ref,
        Stind_Ref,
        Stind_I1,
        Stind_I2,
        Stind_I4,
        Stind_I8,
        Stind_R4,
        Stind_R8,
        Add,
        Sub,
        Mul,
        Div,
        Div_Un,
        Rem,
        Rem_Un,
        And,
        Or,
        Xor,
        Shl,
        Shr,
        Shr_Un,
        Neg,
        Not,
        Conv_I1,
        Conv_I2,
        Conv_I4,
        Conv_I8,
        Conv_R4,
        Conv_R8,
        Conv_U4,
        Conv_U8,
        Callvirt,
        Cpobj,
        Ldobj,
        Ldstr,
        Newobj,
        Castclass,
        Isinst,
        Conv_R_Un,
        Unbox,
        Throw,
        Ldfld,
        Ldflda,
        Stfld,
        Ldsfld,
        Ldsflda,
        Stsfld,
        Stobj,
        Conv_Ovf_I1_Un,
        Conv_Ovf_I2_Un,
        Conv_Ovf_I4_Un,
        Conv_Ovf_I8_Un,
        Conv_Ovf_U1_Un,
        Conv_Ovf_U2_Un,
        Conv_Ovf_U4_Un,
        Conv_Ovf_U8_Un,
        Conv_Ovf_I_Un,
        Conv_Ovf_U_Un,
        Box,
        Newarr,
        Ldlen,
        Ldelema,
        Ldelem_I1,
        Ldelem_U1,
        Ldelem_I2,
        Ldelem_U2,
        Ldelem_I4,
        Ldelem_U4,
        Ldelem_I8,
        Ldelem_I,
        Ldelem_R4,
        Ldelem_R8,
        Ldelem_Ref,
        Stelem_I,
        Stelem_I1,
        Stelem_I2,
        Stelem_I4,
        Stelem_I8,
        Stelem_R4,
        Stelem_R8,
        Stelem_Ref,
        Ldelem_Any,
        Stelem_Any,
        Unbox_Any,
        Conv_Ovf_I1,
        Conv_Ovf_U1,
        Conv_Ovf_I2,
        Conv_Ovf_U2,
        Conv_Ovf_I4,
        Conv_Ovf_U4,
        Conv_Ovf_I8,
        Conv_Ovf_U8,
        Refanyval,
        Ckfinite,
        Mkrefany,
        Ldtoken,
        Conv_U2,
        Conv_U1,
        Conv_I,
        Conv_Ovf_I,
        Conv_Ovf_U,
        Add_Ovf,
        Add_Ovf_Un,
        Mul_Ovf,
        Mul_Ovf_Un,
        Sub_Ovf,
        Sub_Ovf_Un,
        Endfinally,
        Leave,
        Leave_S,
        Stind_I,
        Conv_U,
        Arglist,
        Ceq,
        Cgt,
        Cgt_Un,
        Clt,
        Clt_Un,
        Ldftn,
        Ldvirtftn,
        Ldarg,
        Ldarga,
        Starg,
        Ldloc,
        Ldloca,
        Stloc,
        Localloc,
        Endfilter,
        Unaligned,
        Volatile,
        Tail,
        Initobj,
        Constrained,
        Cpblk,
        Initblk,
        No,
        Rethrow,
        Sizeof,
        Refanytype,
        Readonly,
    }

    public class OpCode
    {
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
        public CodeEx code;
        public int debugline = -1;
        public string debugcode;
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
        public void InitToken(object _p)
        {
            this.tokenUnknown = _p;
            switch (code)
            {
                case CodeEx.Leave:
                case CodeEx.Leave_S:
                case CodeEx.Br:
                case CodeEx.Br_S:
                case CodeEx.Brtrue:
                case CodeEx.Brtrue_S:
                case CodeEx.Brfalse:
                case CodeEx.Brfalse_S:
                //比较流程控制
                case CodeEx.Beq:
                case CodeEx.Beq_S:
                case CodeEx.Bne_Un:
                case CodeEx.Bne_Un_S:
                case CodeEx.Bge:
                case CodeEx.Bge_S:
                case CodeEx.Bge_Un:
                case CodeEx.Bge_Un_S:
                case CodeEx.Bgt:
                case CodeEx.Bgt_S:
                case CodeEx.Bgt_Un:
                case CodeEx.Bgt_Un_S:
                case CodeEx.Ble:
                case CodeEx.Ble_S:
                case CodeEx.Ble_Un:
                case CodeEx.Ble_Un_S:
                case CodeEx.Blt:
                case CodeEx.Blt_S:
                case CodeEx.Blt_Un:
                case CodeEx.Blt_Un_S:
                    //this.tokenAddr = ((Mono.Cecil.Cil.Instruction)_p).Offset;
                    this.tokenAddr_Index = ((Mono.Cecil.Cil.Instruction)_p).Offset;
                    this.tokenValueType = TokenValueType.Addr;
                    break;
                case CodeEx.Isinst:
                case CodeEx.Constrained:
                case CodeEx.Box:
                case CodeEx.Initobj:
                case CodeEx.Castclass:
                case CodeEx.Newarr:
                    this.tokenType = (_p as Mono.Cecil.TypeReference).FullName;
                    this.tokenValueType = TokenValueType.Type;
                    this.tokenUnknown = _p;
                    break;
                case CodeEx.Ldfld:
                case CodeEx.Ldflda:
                case CodeEx.Ldsfld:
                case CodeEx.Ldsflda:
                case CodeEx.Stfld:
                case CodeEx.Stsfld:
                    this.tokenField = (_p as Mono.Cecil.FieldReference).FullName;
                    this.tokenUnknown = _p;
                    this.tokenValueType = TokenValueType.Field;
                    break;
                case CodeEx.Call:
                case CodeEx.Callvirt:
                case CodeEx.Newobj:
                case CodeEx.Ldftn:
                case CodeEx.Ldvirtftn:

                    this.tokenMethod = (_p as Mono.Cecil.MethodReference).FullName;
                    this.tokenUnknown = _p;
                    this.tokenValueType = TokenValueType.Method;
                    break;
                case CodeEx.Ldc_I4:
                    this.tokenI32 = (int)_p;
                    this.tokenValueType = TokenValueType.I32;
                    break;
                case CodeEx.Ldc_I4_S:
                    this.tokenI32 = (int)Convert.ToDecimal(_p);
                    this.tokenValueType = TokenValueType.I32;
                    break;
                case CodeEx.Ldc_I4_M1:
                    this.tokenI32 = -1;
                    this.tokenValueType = TokenValueType.I32;
                    break;
                case CodeEx.Ldc_I4_0:
                    this.tokenI32 = 0;
                    this.tokenValueType = TokenValueType.I32;
                    break;
                case CodeEx.Ldc_I4_1:
                    this.tokenI32 = 1;
                    this.tokenValueType = TokenValueType.I32;
                    break;
                case CodeEx.Ldc_I4_2:
                    this.tokenI32 = 2;
                    this.tokenValueType = TokenValueType.I32;
                    break;
                case CodeEx.Ldc_I4_3:
                    this.tokenI32 = 3;
                    this.tokenValueType = TokenValueType.I32;
                    break;
                case CodeEx.Ldc_I4_4:
                    this.tokenI32 = 4;
                    this.tokenValueType = TokenValueType.I32;
                    break;
                case CodeEx.Ldc_I4_5:
                    this.tokenI32 = 5;
                    this.tokenValueType = TokenValueType.I32;
                    break;
                case CodeEx.Ldc_I4_6:
                    this.tokenI32 = 6;
                    this.tokenValueType = TokenValueType.I32;
                    break;
                case CodeEx.Ldc_I4_7:
                    this.tokenI32 = 7;
                    this.tokenValueType = TokenValueType.I32;
                    break;
                case CodeEx.Ldc_I4_8:
                    this.tokenI32 = 8;
                    this.tokenValueType = TokenValueType.I32;
                    break;
                case CodeEx.Ldc_I8:
                    this.tokenI64 = (Int64)_p;
                    this.tokenValueType = TokenValueType.I64;
                    break;
                case CodeEx.Ldc_R4:
                    this.tokenR32 = (float)_p;
                    this.tokenValueType = TokenValueType.OTher;
                    break;
                case CodeEx.Ldc_R8:
                    this.tokenR64 = (double)_p;
                    this.tokenValueType = TokenValueType.OTher;
                    break;

                case CodeEx.Ldstr:
                    this.tokenStr = _p as string;
                    this.tokenValueType = TokenValueType.String;
                    break;

                case CodeEx.Stloc_0:

                    break;
                case CodeEx.Ldloca:
                case CodeEx.Ldloca_S:
                case CodeEx.Ldloc_S:
                case CodeEx.Stloc_S:
                    this.tokenI32 = ((Mono.Cecil.Cil.VariableDefinition)_p).Index;
                    this.tokenValueType = TokenValueType.I32;

                    //this.tokenUnknown = _p;
                    break;
                case CodeEx.Ldloc:
                case CodeEx.Stloc:
                    this.tokenI32 = (int)_p;
                    this.tokenValueType = TokenValueType.I32;

                    break;
                case CodeEx.Ldloc_0:
                    this.tokenI32 = 0;
                    this.tokenValueType = TokenValueType.I32;

                    break;
                case CodeEx.Ldloc_1:
                    this.tokenI32 = 1;
                    this.tokenValueType = TokenValueType.I32;

                    break;
                case CodeEx.Ldloc_2:
                    this.tokenI32 = 2;
                    this.tokenValueType = TokenValueType.I32;

                    break;
                case CodeEx.Ldloc_3:
                    this.tokenI32 = 3;
                    this.tokenValueType = TokenValueType.I32;

                    break;

                case CodeEx.Ldarga:
                case CodeEx.Ldarga_S:
                case CodeEx.Starg:
                case CodeEx.Starg_S:
                    this.tokenI32 = (_p as Mono.Cecil.ParameterDefinition).Index;
                    this.tokenValueType = TokenValueType.I32;

                    break;
                case CodeEx.Switch:
                    {
                        Mono.Cecil.Cil.Instruction[] e = _p as Mono.Cecil.Cil.Instruction[];
                        tokenAddr_Switch = new int[e.Length];
                        for (int i = 0; i < e.Length; i++)
                        {
                            tokenAddr_Switch[i] = (e[i].Offset);
                        }
                        this.tokenValueType = TokenValueType.AddrArray;

                    }
                    break;
                case CodeEx.Ldarg:
                    this.tokenI32 = (int)_p;
                    this.tokenValueType = TokenValueType.I32;

                    break;
                case CodeEx.Ldarg_S:
                    this.tokenI32 = (_p as Mono.Cecil.ParameterReference).Index;
                    this.tokenValueType = TokenValueType.I32;

                    break;
                case CodeEx.Volatile:
                case CodeEx.Ldind_I1:
                case CodeEx.Ldind_U1:
                case CodeEx.Ldind_I2:
                case CodeEx.Ldind_U2:
                case CodeEx.Ldind_I4:
                case CodeEx.Ldind_U4:
                case CodeEx.Ldind_I8:
                case CodeEx.Ldind_I:
                case CodeEx.Ldind_R4:
                case CodeEx.Ldind_R8:
                case CodeEx.Ldind_Ref:
                    this.tokenValueType = TokenValueType.Nothing;
                    break;

                case CodeEx.Ldtoken:
                    var def = (_p as Mono.Cecil.FieldDefinition);
                    this.tokenUnknown = def.InitialValue;
                    this.tokenValueType = TokenValueType.Nothing;
                    break;
                default:
                    this.tokenUnknown = _p;
                    this.tokenValueType = TokenValueType.Nothing;
                    break;
            }
        }
    }

}
