using System;
using System.Collections.Generic;

namespace Neo.VM.OpCodes
{
    public class JumpTable
    {
        /// <summary>
        /// Opcode
        /// </summary>
        public OpCode OpCode;

        /// <summary>
        /// Action
        /// </summary>
        public Action<ExecutionEngine, ExecutionContext> Execute;

        /// <summary>
        /// Init JumpTable
        /// </summary>
        /// <returns></returns>
        internal static IEnumerable<JumpTable> Init()
        {
            // Default

            for (var opcode = OpCode.PUSHBYTES1; opcode <= OpCode.PUSHBYTES75; opcode++)
            {
                yield return new JumpTable()
                {
                    OpCode = opcode,
                    Execute = (engine, context) =>
                    {
                        context.EvaluationStack.Push(context.OpReader.SafeReadBytes((byte)opcode));
                    }
                };
            }

            // Push value

            yield return new JumpTable()
            {
                OpCode = OpCode.PUSH0,
                Execute = (engine, context) =>
                {
                    context.EvaluationStack.Push(new byte[0]);
                }
            };
        }
    }
}