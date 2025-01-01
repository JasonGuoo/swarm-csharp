using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Swarm.CSharp.LLM.Models
{
    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("tool_name")]
        public string ToolName { get; set; }

        [JsonPropertyName("tool_call_id")]
        public string ToolCallId { get; set; }

        [JsonPropertyName("tool_calls")]
        public ToolCall[] ToolCalls { get; set; }

        [JsonIgnore]
        private JsonElement? _rawJson;

        [JsonExtensionData]
        public Dictionary<string, JsonElement> AdditionalProperties { get; set; }

        public Message()
        {
            AdditionalProperties = new Dictionary<string, JsonElement>();
        }

        public T GetFieldValue<T>(string path)
        {
            if (_rawJson == null || string.IsNullOrEmpty(path))
            {
                return default;
            }

            try
            {
                var pathParts = path.Split('/');
                JsonElement current = _rawJson.Value;

                foreach (var part in pathParts)
                {
                    if (!current.TryGetProperty(part, out current))
                    {
                        return default;
                    }
                }

                return JsonSerializer.Deserialize<T>(current.GetRawText());
            }
            catch
            {
                return default;
            }
        }

        public void SetRawJson(JsonElement rawJson)
        {
            _rawJson = rawJson;
        }
    }

    public class ToolCall
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("index")]
        public int? Index { get; set; }

        [JsonPropertyName("function")]
        public FunctionCall Function { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> AdditionalProperties { get; set; }

        public ToolCall()
        {
            AdditionalProperties = new Dictionary<string, JsonElement>();
        }
    }

    public class FunctionCall
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("arguments")]
        public string Arguments { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> AdditionalProperties { get; set; }

        public FunctionCall()
        {
            AdditionalProperties = new Dictionary<string, JsonElement>();
        }
    }
}