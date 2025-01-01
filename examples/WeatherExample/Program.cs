using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Swarm.CSharp.Core;
using Swarm.CSharp.LLM;
using Swarm.CSharp.LLM.Models;
using Swarm.CSharp.LLM.Providers;
using Swarm.CSharp.Utils;

namespace WeatherExample;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            // Load environment variables
            EnvLoader.Load();

            var host = CreateHostBuilder(args).Build();
            await RunExampleAsync(host.Services);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Fatal error occurred");
            Environment.Exit(1);
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                      .AddJsonFile("appsettings.json", optional: false)
                      .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true)
                      .AddEnvironmentVariables()
                      .AddCommandLine(args);
            })
            .ConfigureServices((hostContext, services) =>
            {
                // Configure LLM client using environment variables
                services.AddSingleton<ILLMClient>(sp =>
                {
                    var apiKey = EnvLoader.GetEnvVar("OPENAI_API_KEY") ??
                        throw new InvalidOperationException("OpenAI API key not found in environment variables");
                    var model = EnvLoader.GetEnvVar("OPENAI_MODEL") ?? "gpt-3.5-turbo";
                    var base_url = EnvLoader.GetEnvVar("OPENAI_BASE_URL") ?? "https://api.openai.com/v1";

                    return new OpenAIClient(apiKey, model, base_url);
                });

                // Add SwarmCore logger
                services.AddSingleton<ILogger<SwarmCore>>(sp =>
                {
                    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                    return loggerFactory.CreateLogger<SwarmCore>();
                });
            })
            .ConfigureLogging((hostContext, logging) =>
            {
                logging.ClearProviders()
                       .AddConsole()
                       .AddDebug()
                       .SetMinimumLevel(LogLevel.Debug);
            });

    private static async Task RunExampleAsync(IServiceProvider services)
    {
        var llmClient = services.GetRequiredService<ILLMClient>();
        var swarmLogger = services.GetRequiredService<ILogger<SwarmCore>>();
        var swarm = new SwarmCore(llmClient, swarmLogger);
        var agent = new WeatherAgent();

        // Initialize context with preferences
        var context = new Dictionary<string, object>
        {
            ["temperature_unit"] = "fahrenheit",
            ["email_signature"] = "\n\nBest regards,\nWeather Bot"
        };

        try
        {
            // Test Scenario 1: Basic weather query
            Logger.LogInformation("Test Scenario 1: Basic weather query");
            var weatherQuery = new Message
            {
                Role = "user",
                Content = "What's the weather like in San Francisco?"
            };

            var response1 = await swarm.RunAsync(
                agent,
                new List<Message> { weatherQuery },
                context,
                modelOverride: null,
                stream: false,
                debug: true,
                maxTurns: 10);
            Logger.LogInformation($"Response of Scenario 1: {response1.History[^1].Content}");

            // Test Scenario 2: Weather + Email
            Logger.LogInformation("\nTest Scenario 2: Weather + Email combination");
            var emailQuery = new Message
            {
                Role = "user",
                Content = "What's the weather like in San Francisco? Send an email about it to test@example.com"
            };

            var response2 = await swarm.RunAsync(
                agent,
                new List<Message> { emailQuery },
                context,
                modelOverride: null,
                stream: false,
                debug: true,
                maxTurns: 10);
            Logger.LogInformation($"Response of Scenario 2: {response2.History[^1].Content}");

            // Test Scenario 3: Streaming example
            Logger.LogInformation("\nTest Scenario 3: Streaming weather updates");
            var streamQuery = new Message
            {
                Role = "user",
                Content = "Give me detailed weather information for New York City"
            };

            var response3 = await swarm.RunAsync(
                agent,
                new List<Message> { streamQuery },
                context,
                modelOverride: null,
                stream: true,
                debug: true,
                maxTurns: 10);
            Logger.LogInformation($"Response of Scenario 3: {response3.History[^1].Content}");

            // Test Scenario 4: Error handling
            Logger.LogInformation("\nTest Scenario 4: Error handling");
            var errorQuery = new Message
            {
                Role = "user",
                Content = "Send weather update to invalid-email"
            };

            try
            {
                await swarm.RunAsync(
                    agent,
                    new List<Message> { errorQuery },
                    context,
                    modelOverride: null,
                    stream: false,
                    debug: true,
                    maxTurns: 10);
            }
            catch (Exception e)
            {
                Logger.LogError($"Expected error occurred: {e.Message}");
            }

            // Print final context state
            Logger.LogInformation("\nFinal context state:");
            foreach (var (key, value) in context)
            {
                Logger.LogInformation($"{key}: {value}");
            }
        }
        catch (Exception e)
        {
            Logger.LogError($"Error running weather example: {e.Message}");
            throw;
        }
    }
}
