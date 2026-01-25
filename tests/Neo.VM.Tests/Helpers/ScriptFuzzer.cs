// Copyright (C) 2015-2026 The Neo Project.
//
// ScriptFuzzer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using System;
using System.Collections.Generic;

namespace Neo.Test.Helpers;

public static class ScriptFuzzer
{
    private static readonly OpCode[] _ops = new[]
    {
        OpCode.NOP,
        OpCode.PUSH0,
        OpCode.PUSH1,
        OpCode.PUSH2,
        OpCode.ADD,
        OpCode.SUB,
        OpCode.MUL,
        OpCode.NOT,
        OpCode.DUP,
        OpCode.SWAP,
        OpCode.DROP
    };

    public static IEnumerable<byte[]> GenerateScripts(int seed, int scriptCount, int maxLength)
    {
        var rand = new Random(seed);
        for (int i = 0; i < scriptCount; i++)
        {
            using var builder = new ScriptBuilder();
            var length = rand.Next(1, maxLength + 1);
            for (int j = 0; j < length; j++)
            {
                var op = _ops[rand.Next(_ops.Length)];
                builder.Emit(op);
            }
            builder.Emit(OpCode.RET);
            yield return builder.ToArray();
        }
    }
}
