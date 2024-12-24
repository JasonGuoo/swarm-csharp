using System.Net;
using System.Net.Http;
using System.Text;

namespace Swarm.CSharp.Tests.Mocks;

public static class MockResponses
{
    public static HttpResponseMessage CreateChatCompletionResponse(string content)
    {
        var json = $@"{{
            ""id"": ""mock-response-id"",
            ""object"": ""chat.completion"",
            ""created"": 1703399264,
            ""model"": ""gpt-3.5-turbo"",
            ""choices"": [
                {{
                    ""index"": 0,
                    ""message"": {{
                        ""role"": ""assistant"",
                        ""content"": ""{content}""
                    }},
                    ""finish_reason"": ""stop""
                }}
            ],
            ""usage"": {{
                ""prompt_tokens"": 10,
                ""completion_tokens"": 20,
                ""total_tokens"": 30
            }}
        }}";

        return new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    public static HttpResponseMessage CreateFunctionCallResponse(string functionName, string arguments)
    {
        var json = $@"{{
            ""id"": ""mock-response-id"",
            ""object"": ""chat.completion"",
            ""created"": 1703399264,
            ""model"": ""gpt-3.5-turbo"",
            ""choices"": [
                {{
                    ""index"": 0,
                    ""message"": {{
                        ""role"": ""assistant"",
                        ""content"": null,
                        ""function_call"": {{
                            ""name"": ""{functionName}"",
                            ""arguments"": ""{arguments}""
                        }}
                    }},
                    ""finish_reason"": ""function_call""
                }}
            ],
            ""usage"": {{
                ""prompt_tokens"": 10,
                ""completion_tokens"": 20,
                ""total_tokens"": 30
            }}
        }}";

        return new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    public static HttpResponseMessage CreateErrorResponse(HttpStatusCode statusCode, string message)
    {
        var json = $@"{{
            ""error"": {{
                ""message"": ""{message}"",
                ""type"": ""invalid_request_error"",
                ""code"": ""invalid_api_key""
            }}
        }}";

        return new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
}
