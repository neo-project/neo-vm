using Neo.Test.Converters;
using Neo.VM;
using Newtonsoft.Json;

namespace Neo.Test.Types
{
    public class VMUTExecutionContextState
    {
        [JsonProperty, JsonConverter(typeof(ScriptConverter))]
        public byte[] ScriptHash { get; set; }

        [JsonProperty]
        public OpCode NextInstruction { get; set; }

        [JsonProperty]
        public int InstructionPointer { get; set; }

        // Stacks

        [JsonProperty]
        public VMUTStackItem[] AltStack { get; set; }

        [JsonProperty]
        public VMUTStackItem[] EvaluationStack { get; set; }
    }
}