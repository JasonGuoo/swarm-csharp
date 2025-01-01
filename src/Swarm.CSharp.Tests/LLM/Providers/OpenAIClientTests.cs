using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Swarm.CSharp.LLM.Models;
using Swarm.CSharp.LLM.Providers;
using Xunit;
using Swarm.CSharp.Tests.Helpers;

namespace Swarm.CSharp.Tests.LLM.Providers
{
    public class OpenAIClientTests
    {
        private readonly Mock<ILogger<OpenAIClient>> _loggerMock;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _baseUrl;

        public OpenAIClientTests()
        {
            // Load environment variables
            EnvLoader.Load();

            _apiKey = EnvLoader.GetEnvVar("OPENAI_API_KEY", "test-key");
            _model = EnvLoader.GetEnvVar("OPENAI_MODEL", "gpt-4o-mini");
            _baseUrl = EnvLoader.GetEnvVar("OPENAI_BASE_URL", "https://api.openai.com/v1");

            _loggerMock = new Mock<ILogger<OpenAIClient>>();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        private OpenAIClient CreateClient(HttpMessageHandler handler)
        {
            return new OpenAIClient(
                apiKey: _apiKey,
                model: _model,
                baseUrl: _baseUrl
            );
        }
        
    }
}