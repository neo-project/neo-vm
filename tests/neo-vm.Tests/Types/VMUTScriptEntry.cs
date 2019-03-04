using Neo.Test.Converters;
using Newtonsoft.Json;

namespace Neo.Test.Types
{
    public class VMUTScriptEntry
    {
        [JsonProperty, JsonConverter(typeof(ScriptConverter))]
        public byte[] Script { get; set; }
    }
}