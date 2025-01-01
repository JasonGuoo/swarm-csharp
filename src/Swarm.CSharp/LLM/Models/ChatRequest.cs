using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Swarm.CSharp.LLM.Models
{
    public class ChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; }

        [JsonPropertyName("functions")]
        public List<FunctionSchema> Functions { get; set; }

        [JsonPropertyName("function_call")]
        public string FunctionCall { get; set; }

        [JsonPropertyName("tools")]
        public List<Tool> Tools { get; set; }

        [JsonPropertyName("tool_choice")]
        public string ToolChoice { get; set; }

        [JsonPropertyName("temperature")]
        public float? Temperature { get; set; }

        [JsonPropertyName("stream")]
        public bool? Stream { get; set; }

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }

        public ChatRequest()
        {
            Messages = new List<Message>();
            Functions = new List<FunctionSchema>();
            Tools = new List<Tool>();
            ToolChoice = "auto";
        }

        public ChatRequest(string model, List<Message> messages)
        {
            Model = model;
            Messages = messages ?? new List<Message>();
            Functions = new List<FunctionSchema>();
            Tools = new List<Tool>();
            ToolChoice = "auto";
        }
    }

    public class Tool
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "function";

        [JsonPropertyName("function")]
        public required FunctionSchema Function { get; set; }
    }
}