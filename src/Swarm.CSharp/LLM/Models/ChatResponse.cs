using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Swarm.CSharp.LLM.Models
{
    public class ChatResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("object")]
        public string ObjectName { get; set; }

        [JsonPropertyName("created")]
        public int Created { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; }

        [JsonPropertyName("usage")]
        public Usage Usage { get; set; }

        [JsonPropertyName("request_id")]
        public string RequestId { get; set; }

        [JsonIgnore]
        private JsonElement? _rawJson;

        [JsonExtensionData]
        public Dictionary<string, JsonElement> AdditionalProperties { get; set; }

        public ChatResponse(string id, string objectName, int created, string model, List<Choice> choices, Usage usage)
        {
            Id = id;
            ObjectName = objectName;
            Created = created;
            Model = model;
            Choices = choices;
            Usage = usage;
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

    public class Choice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public Message Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }

        public Choice(int index, Message message, string finishReason)
        {
            Index = index;
            Message = message;
            FinishReason = finishReason;
        }
    }
}