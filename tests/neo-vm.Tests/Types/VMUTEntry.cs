using Neo.Test.Converters;
using Newtonsoft.Json;

namespace Neo.Test.Types
{
    public class VMUTEntry
    {
        [JsonProperty, JsonConverter(typeof(ScriptConverter))]
        public byte[] Script { get; set; }

        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public VMUTStep[] Steps { get; set; }
    }
}
