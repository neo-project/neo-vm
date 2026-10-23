// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Script.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using System;
using System.Text;

namespace Neo.Test;

[TestClass]
public class UT_Script
{
    [TestMethod]
    [DataRow(OpCode.PUSHA)]
    [DataRow(OpCode.JMP_L)]
    [DataRow(OpCode.JMPIF_L)]
    [DataRow(OpCode.JMPIFNOT_L)]
    [DataRow(OpCode.JMPEQ_L)]
    [DataRow(OpCode.JMPNE_L)]
    [DataRow(OpCode.JMPGT_L)]
    [DataRow(OpCode.JMPGE_L)]
    [DataRow(OpCode.JMPLT_L)]
    [DataRow(OpCode.JMPLE_L)]
    [DataRow(OpCode.CALL_L)]
    [DataRow(OpCode.ENDTRY_L)]
    [DataRow(OpCode.TRY_L)]
    public void StrictModeRejectsInvalidLongTargets(OpCode opcode)
    {
        foreach (int offset in new[] { int.MaxValue, int.MinValue, -2, 2, 100 })
        {
            byte[] bytes = new byte[opcode == OpCode.TRY_L ? 10 : 6];
            bytes[0] = (byte)OpCode.NOP;
            bytes[1] = (byte)opcode;
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(2), offset);
            Assert.ThrowsExactly<BadScriptException>(() => _ = new Script(bytes, true));
            if (opcode == OpCode.TRY_L)
            {
                // Validate the finally offset independently of the catch offset.
                System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(2), 0);
                System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(6), offset);
                Assert.ThrowsExactly<BadScriptException>(() => _ = new Script(bytes, true));
            }
        }
    }

    [TestMethod]
    public void StrictModeAcceptsValidRelativeTargets()
    {
        byte[] bytes = [(byte)OpCode.NOP, (byte)OpCode.JMP_L, 0xFF, 0xFF, 0xFF, 0xFF];
        _ = new Script(bytes, true);
        bytes = [(byte)OpCode.TRY_L, 9, 0, 0, 0, 10, 0, 0, 0, (byte)OpCode.NOP, (byte)OpCode.RET];
        _ = new Script(bytes, true);
    }

    [TestMethod]
    public void TestConversion()
    {
        byte[] rawScript;
        using (var builder = new ScriptBuilder())
        {
            builder.Emit(OpCode.PUSH0);
            builder.Emit(OpCode.CALL, new byte[] { 0x00, 0x01 });
            builder.EmitSysCall(123);

            rawScript = builder.ToArray();
        }

        var script = new Script(rawScript);

        ReadOnlyMemory<byte> scriptConversion = script;
        Assert.AreEqual(rawScript, scriptConversion);
    }

    [TestMethod]
    public void TestStrictMode()
    {
        var rawScript = new byte[] { (byte)OpCode.PUSH0, 0xFF };
        Assert.ThrowsExactly<BadScriptException>(() => _ = new Script(rawScript, true));

        var script = new Script(rawScript, false);
        Assert.AreEqual(2, script.Length);

        rawScript = new byte[] { (byte)OpCode.PUSHDATA1 };
        Assert.ThrowsExactly<BadScriptException>(() => _ = new Script(rawScript, true));

        rawScript = new byte[] { (byte)OpCode.PUSHDATA2 };
        Assert.ThrowsExactly<BadScriptException>(() => _ = new Script(rawScript, true));

        rawScript = new byte[] { (byte)OpCode.PUSHDATA4 };
        Assert.ThrowsExactly<BadScriptException>(() => _ = new Script(rawScript, true));
    }

    [TestMethod]
    public void TestParse()
    {
        Script script;

        using (var builder = new ScriptBuilder())
        {
            builder.Emit(OpCode.PUSH0);
            builder.Emit(OpCode.CALL_L, new byte[] { 0x00, 0x01, 0x00, 0x00 });
            builder.EmitSysCall(123);

            script = new Script(builder.ToArray());
        }

        Assert.AreEqual(11, script.Length);

        var ins = script.GetInstruction(0);

        Assert.AreEqual(OpCode.PUSH0, ins.OpCode);
        Assert.IsTrue(ins.Operand.IsEmpty);
        Assert.AreEqual(1, ins.Size);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { var x = ins.TokenI16; });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { var x = ins.TokenU32; });

        ins = script.GetInstruction(1);

        Assert.AreEqual(OpCode.CALL_L, ins.OpCode);
        CollectionAssert.AreEqual(new byte[] { 0x00, 0x01, 0x00, 0x00 }, ins.Operand.ToArray());
        Assert.AreEqual(5, ins.Size);
        Assert.AreEqual(256, ins.TokenI32);
        Assert.AreEqual(Encoding.ASCII.GetString(new byte[] { 0x00, 0x01, 0x00, 0x00 }), ins.TokenString);

        ins = script.GetInstruction(6);

        Assert.AreEqual(OpCode.SYSCALL, ins.OpCode);
        CollectionAssert.AreEqual(new byte[] { 123, 0x00, 0x00, 0x00 }, ins.Operand.ToArray());
        Assert.AreEqual(5, ins.Size);
        Assert.AreEqual(123, ins.TokenI16);
        Assert.AreEqual(Encoding.ASCII.GetString(new byte[] { 123, 0x00, 0x00, 0x00 }), ins.TokenString);
        Assert.AreEqual(123U, ins.TokenU32);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = script.GetInstruction(100));
    }
}
