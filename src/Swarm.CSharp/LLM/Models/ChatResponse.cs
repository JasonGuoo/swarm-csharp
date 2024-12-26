using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Swarm.CSharp.LLM.Models
{
    public class ChatResponse
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("object")]
        public required string Object { get; set; }

        [JsonPropertyName("created")]
        public required long Created { get; set; }

        [JsonPropertyName("model")]
        public required string Model { get; set; }

        [JsonPropertyName("choices")]
        public required List<Choice> Choices { get; set; } = new();

        [JsonPropertyName("usage")]
        public required Usage Usage { get; set; }

        public class Choice
        {
            [JsonPropertyName("index")]
            public required int Index { get; set; }

            [JsonPropertyName("message")]
            public required Message Message { get; set; }

            [JsonPropertyName("finish_reason")]
            public required string FinishReason { get; set; }
        }
    }
}