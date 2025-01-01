using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Swarm.CSharp.Core;
using Swarm.CSharp.LLM;
using Swarm.CSharp.LLM.Models;
using Swarm.CSharp.LLM.Providers;

namespace WeatherExample;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            var host = CreateHostBuilder(args).Build();
            await RunExampleAsync(host.Services);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
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
                // Configure LLM client
                services.AddSingleton<ILLMClient>(sp =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    var apiKey = config["OpenAI:ApiKey"] ?? 
                        throw new InvalidOperationException("OpenAI API key not configured");
                    
                    return new OpenAIClient(apiKey);
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
        var logger = services.GetRequiredService<ILogger<Program>>();
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
            logger.LogInformation("Test Scenario 1: Basic weather query");
            var weatherQuery = new Message
            {
                Role = "user",
                Content = "What's the weather like in San Francisco?"
            };

            var response1 = await swarm.RunAsync(
                agent,                     // The agent to use
                new List<Message> { weatherQuery }, // List of messages
                context,                   // Context map
                modelOverride: null,        // Model override (optional)
                stream: false,              // Streaming mode
                debug: true,                // Debug mode
                maxTurns: 10);             // Max conversation turns
            logger.LogInformation("Response of Scenario 1: {Response}", response1.History[^1].Content);

            // Test Scenario 2: Weather + Email
            logger.LogInformation("\nTest Scenario 2: Weather + Email combination");
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
            logger.LogInformation("Response of Scenario 2: {Response}", response2.History[^1].Content);

            // Test Scenario 3: Streaming example
            logger.LogInformation("\nTest Scenario 3: Streaming weather updates");
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
            logger.LogInformation("Response of Scenario 3: {Response}", response3.History[^1].Content);

            // Test Scenario 4: Error handling
            logger.LogInformation("\nTest Scenario 4: Error handling");
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
                logger.LogError("Expected error occurred: {Message}", e.Message);
            }

            // Print final context state
            logger.LogInformation("\nFinal context state:");
            foreach (var (key, value) in context)
            {
                logger.LogInformation("{Key}: {Value}", key, value);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error running weather example");
            throw;
        }
    }
}
