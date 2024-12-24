using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Swarm.CSharp.Tests.Mocks;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri?.Host.Contains("openai.azure.com") == true)
        {
            if (!request.Headers.Contains("api-key"))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("Missing Azure OpenAI API key")
                });
            }
        }
        else if (request.RequestUri?.Host.Contains("openai.com") == true)
        {
            if (!request.Headers.Contains("Authorization") || !request.Headers.Authorization!.Parameter!.StartsWith("sk-"))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("Invalid OpenAI API key")
                });
            }
        }
        else if (request.RequestUri?.Host.Contains("chatglm.com") == true)
        {
            if (!request.Headers.Contains("Authorization") || !request.Headers.Authorization!.Parameter!.Contains("."))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("Invalid ChatGLM API key format")
                });
            }
        }
        else if (request.RequestUri?.Host.Contains("ollama.com") == true)
        {
            // Ollama doesn't require authentication, but we'll validate the model endpoint
            if (request.RequestUri.PathAndQuery.Contains("/api/chat") && !request.Content!.Headers.Contains("model"))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Missing model parameter")
                });
            }
        }

        try
        {
            return Task.FromResult(_handler(request));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(ex.Message)
            });
        }
    }
}
