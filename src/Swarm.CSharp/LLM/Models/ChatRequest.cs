using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Swarm.CSharp.LLM.Models
{
    public class ChatRequest
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("messages")]
        public required List<Message> Messages { get; set; } = new();

        [JsonPropertyName("tools")]
        public List<Tool> Tools { get; set; } = new();

        [JsonPropertyName("tool_choice")]
        public string ToolChoice { get; set; } = "auto";

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }
    }

    public class Tool
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "function";

        [JsonPropertyName("function")]
        public required FunctionSchema Function { get; set; }
    }
}