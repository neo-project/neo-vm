// Copyright (C) 2015-2026 The Neo Project.
// Distributed under the MIT software license.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using System;
using System.Globalization;
using System.Text;

namespace Neo.Test;

[TestClass]
public class UT_CatchableExceptionCulture
{
    [TestMethod]
    [DataRow("en-US", OpCode.NEWARRAY0, "The index of VMArray is out of range, -1/[0, 0).")]
    [DataRow("sv-SE", OpCode.NEWARRAY0, "The index of VMArray is out of range, -1/[0, 0).")]
    [DataRow("en-US", OpCode.NEWMAP, "Key -1 not found in Map.")]
    [DataRow("sv-SE", OpCode.NEWMAP, "Key -1 not found in Map.")]
    public void PickItemCaughtTextIsCultureInvariant(string culture, OpCode collection, string expected)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            using ScriptBuilder script = new();
            script.Emit(OpCode.TRY, new byte[] { 6, 0 });
            script.Emit(collection);
            script.Emit(OpCode.PUSHM1);
            script.Emit(OpCode.PICKITEM);
            script.Emit(OpCode.RET);

            using ExecutionEngine engine = new();
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(expected, Encoding.UTF8.GetString(engine.ResultStack.Peek().GetSpan()));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
