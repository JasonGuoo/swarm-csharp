using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Swarm.CSharp.LLM.Models
{
    public class FunctionSchema
    {
        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("description")]
        public required string Description { get; set; }

        [JsonPropertyName("parameters")]
        public required Dictionary<string, object> Parameters { get; set; }
    }
}