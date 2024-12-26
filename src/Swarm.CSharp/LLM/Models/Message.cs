using System.Text.Json.Serialization;

namespace Swarm.CSharp.LLM.Models
{
    public class Message
    {
        [JsonPropertyName("role")]
        public required string Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("tool_calls")]
        public ToolCall[]? ToolCalls { get; set; }

        [JsonPropertyName("tool_call_id")]
        public string? ToolCallId { get; set; }

        [JsonPropertyName("tool_name")]
        public string? ToolName { get; set; }

        public class ToolCall
        {
            [JsonPropertyName("id")]
            public required string Id { get; set; }

            [JsonPropertyName("type")]
            public required string Type { get; set; }

            [JsonPropertyName("function")]
            public required FunctionCall Function { get; set; }
        }

        public class FunctionCall
        {
            [JsonPropertyName("name")]
            public required string Name { get; set; }

            [JsonPropertyName("arguments")]
            public required string Arguments { get; set; }
        }
    }
}