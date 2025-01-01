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

        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        [JsonPropertyName("top_p")]
        public double? TopP { get; set; }

        [JsonPropertyName("n")]
        public int? N { get; set; }

        [JsonPropertyName("stream")]
        public bool? Stream { get; set; }

        [JsonPropertyName("stop")]
        public List<string> Stop { get; set; }

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }

        [JsonPropertyName("presence_penalty")]
        public double? PresencePenalty { get; set; }

        [JsonPropertyName("frequency_penalty")]
        public double? FrequencyPenalty { get; set; }

        [JsonPropertyName("logit_bias")]
        public Dictionary<string, int> LogitBias { get; set; }

        [JsonPropertyName("user")]
        public string User { get; set; }

        [JsonPropertyName("tools")]
        public List<Tool> Tools { get; set; }

        [JsonPropertyName("tool_choice")]
        public object ToolChoice { get; set; }

        // For backward compatibility
        [JsonIgnore]
        public List<FunctionSchema> Functions
        {
            get => Tools?.ConvertAll(t => t.Function);
            set => Tools = value?.ConvertAll(f => new Tool { Type = "function", Function = f });
        }

        [JsonIgnore]
        public object FunctionCall
        {
            get => ToolChoice;
            set => ToolChoice = ConvertFunctionCallToToolChoice(value);
        }

        public ChatRequest()
        {
            Messages = new List<Message>();
            Tools = new List<Tool>();
            ToolChoice = "auto";
        }

        public ChatRequest(string model, List<Message> messages) : this()
        {
            Model = model;
            Messages = messages ?? new List<Message>();
        }

        private object ConvertFunctionCallToToolChoice(object functionCall)
        {
            if (functionCall == null) return null;
            if (functionCall is string str && str == "auto")
            {
                return "auto";
            }
            if (functionCall is Dictionary<string, object> dict)
            {
                return new { type = "function", function = dict };
            }
            return functionCall;
        }
    }

    public class Tool
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "function";

        [JsonPropertyName("function")]
        public required FunctionSchema Function { get; set; }

        public static Tool FromFunctionSchema(FunctionSchema schema)
        {
            return new Tool { Function = schema };
        }
    }
}