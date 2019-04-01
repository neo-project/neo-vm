using System;
using System.Text;
using Neo.Test.Extensions;
using Neo.Test.Types;
using Neo.VM;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Neo.Test
{
    public abstract class VMJsonTestBase
    {
        /// <summary>
        /// Execute this test
        /// </summary>
        /// <param name="ut">Test</param>
        public void ExecuteTest(VMUT ut)
        {
            foreach (var test in ut.Tests)
            {
                // Interop service

                IInteropService service = new InteropService();

                // Message provider

                IScriptContainer scriptContainer = null;

                if (test.Message != null)
                {
                    scriptContainer = new MessageProvider(test.Message);
                }


                // Script table

                ScriptTable scriptTable = null;

                if (test.ScriptTable != null)
                {
                    scriptTable = new ScriptTable();

                    foreach (var script in test.ScriptTable)
                    {
                        scriptTable.Add(script.Script);
                    }
                }

                // Create engine

                using (var engine = new ExecutionEngine(scriptContainer, Crypto.Default, scriptTable, service))
                {
                    Debugger debugger = new Debugger(engine);
                    engine.LoadScript(test.Script);

                    // Execute Steps

                    if (test.Steps != null)
                    {
                        foreach (var step in test.Steps)
                        {
                            // Actions

                            if (step.Actions != null) foreach (var run in step.Actions)
                                {
                                    switch (run)
                                    {
                                        case VMUTActionType.Execute: debugger.Execute(); break;
                                        case VMUTActionType.StepInto: debugger.StepInto(); break;
                                        case VMUTActionType.StepOut: debugger.StepOut(); break;
                                        case VMUTActionType.StepOver: debugger.StepOver(); break;
                                    }
                                }

                            // Review results

                            var add = string.IsNullOrEmpty(step.Name) ? "" : "-" + step.Name;

                            AssertResult(engine, step.Result, $"{ut.Category}-{ut.Name}-{test.Name}{add}: ");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Assert result
        /// </summary>
        /// <param name="engine">Engine</param>
        /// <param name="result">Result</param>
        /// <param name="message">Message</param>
        private void AssertResult(ExecutionEngine engine, VMUTExecutionEngineState result, string message)
        {
            AssertAreEqual(engine.State.ToString().ToLowerInvariant(), result.State.ToString().ToLowerInvariant(), message + "State is different");

            AssertResult(engine.InvocationStack, result.InvocationStack, message + " [Invocation stack]");
            AssertResult(engine.ResultStack, result.ResultStack, message + " [Result stack] ");
        }

        /// <summary>
        /// Assert invocation stack
        /// </summary>
        /// <param name="stack">Stack</param>
        /// <param name="result">Result</param>
        /// <param name="message">Message</param>
        private void AssertResult(RandomAccessStack<ExecutionContext> stack, VMUTExecutionContextState[] result, string message)
        {
            AssertAreEqual(stack.Count, result == null ? 0 : result.Length, message + "Stack is different");

            for (int x = 0, max = stack.Count; x < max; x++)
            {
                var context = stack.Peek(x);
                var opcode = context.InstructionPointer >= context.Script.Length ? OpCode.RET : context.Script[context.InstructionPointer];

                AssertAreEqual(context.ScriptHash.ToHexString().ToUpper(), result[x].ScriptHash.ToHexString().ToUpper(), message + "Script hash is different");
                AssertAreEqual(opcode, result[x].NextInstruction, message + "Next instruction is different");
                AssertAreEqual(context.InstructionPointer, result[x].InstructionPointer, message + "Instruction pointer is different");

                AssertResult(context.EvaluationStack, result[x].EvaluationStack, message + " [EvaluationStack]");
                AssertResult(context.AltStack, result[x].AltStack, message + " [AltStack]");
            }
        }

        /// <summary>
        /// Assert result stack
        /// </summary>
        /// <param name="stack">Stack</param>
        /// <param name="result">Result</param>
        /// <param name="message">Message</param>
        private void AssertResult(RandomAccessStack<StackItem> stack, VMUTStackItem[] result, string message)
        {
            AssertAreEqual(stack.Count, result == null ? 0 : result.Length, message + "Stack is different");

            for (int x = 0, max = stack.Count; x < max; x++)
            {
                AssertAreEqual(ItemToJson(stack.Peek(x)).ToString(Formatting.None), PrepareJsonItem(result[x]).ToString(Formatting.None), message + "Stack item is different");
            }
        }

        private JObject PrepareJsonItem(VMUTStackItem item)
        {
            var ret = new JObject
            {
                ["type"] = item.Type.ToString(),
                ["value"] = item.Value
            };

            switch (item.Type)
            {
                case VMUTStackItemType.String:
                    {
                        // Easy access

                        ret["type"] = VMUTStackItemType.ByteArray.ToString();
                        ret["value"] = Encoding.UTF8.GetBytes(item.Value.Value<string>());
                        break;
                    }
                case VMUTStackItemType.ByteArray:
                    {
                        var value = ret["value"].Value<string>();

                        if (value.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
                        {
                            ret["value"] = value.FromHexString();
                        }
                        else
                        {
                            ret["value"] = Convert.FromBase64String(value);
                        }

                        break;
                    }
                case VMUTStackItemType.Integer:
                    {
                        // Ensure format

                        ret["value"] = ret["value"].Value<string>();
                        break;
                    }
                case VMUTStackItemType.Struct:
                case VMUTStackItemType.Array:
                    {
                        var array = (JArray)ret["value"];

                        for (int x = 0, m = array.Count; x < m; x++)
                        {
                            array[x] = PrepareJsonItem(JsonConvert.DeserializeObject<VMUTStackItem>(array[x].ToString()));
                        }

                        ret["value"] = array;
                        break;
                    }
                case VMUTStackItemType.Map:
                    {
                        var obj = (JObject)ret["value"];

                        foreach (var prop in obj.Properties())
                        {
                            obj[prop.Name] = PrepareJsonItem(JsonConvert.DeserializeObject<VMUTStackItem>(prop.Value.ToString()));
                        }

                        ret["value"] = obj;
                        break;
                    }
            }

            return ret;
        }

        private JToken ItemToJson(StackItem item)
        {
            if (item == null) return null;

            JToken value;
            string type = item.GetType().Name;

            switch (item)
            {
                case VM.Types.Boolean v: value = new JValue(v.GetBoolean()); break;
                case VM.Types.Integer v: value = new JValue(v.GetBigInteger().ToString()); break;
                case VM.Types.ByteArray v: value = new JValue(v.GetByteArray()); break;
                //case VM.Types.Struct v:
                case VM.Types.Array v:
                    {
                        var jarray = new JArray();

                        foreach (var entry in v)
                        {
                            jarray.Add(ItemToJson(entry));
                        }

                        value = jarray;
                        break;
                    }
                case VM.Types.Map v:
                    {
                        var jdic = new JObject();

                        foreach (var entry in v)
                        {
                            jdic.Add(entry.Key.GetByteArray().ToHexString(), ItemToJson(entry.Value));
                        }

                        value = jdic;
                        break;
                    }
                case VM.Types.InteropInterface v:
                    {
                        type = "Interop";
                        var obj = v.GetInterface<object>();

                        value = obj.GetType().Name.ToString();
                        break;
                    }
                default: throw new NotImplementedException();
            }

            return new JObject
            {
                ["type"] = type,
                ["value"] = value
            };
        }

        /// <summary>
        /// Assert with message
        /// </summary>
        /// <param name="a">A</param>
        /// <param name="b">B</param>
        /// <param name="message">Message</param>
        private void AssertAreEqual(object a, object b, string message)
        {
            if (a is byte[] ba) a = ba.ToHexString().ToUpperInvariant();
            if (b is byte[] bb) b = bb.ToHexString().ToUpperInvariant();

            if (a.ToJson() != b.ToJson())
            {
                throw new Exception(message +
                    $"{Environment.NewLine}Expected:{Environment.NewLine + a.ToString() + Environment.NewLine}Actual:{Environment.NewLine + b.ToString()}");
            }
        }
    }
}