using System.Text.RegularExpressions;
using Swarm.CSharp.Core;
using Swarm.CSharp.Function.Attributes;

namespace WeatherExample;

/// <summary>
/// WeatherAgent - Example implementation of a Swarm Agent
/// 
/// This class demonstrates how to create a custom agent using the Swarm framework.
/// Key components of an agent implementation:
/// 1. Extend Agent base class
/// 2. Override GetSystemPrompt
/// 3. Define tool functions using FunctionSpec attribute
/// </summary>
public class WeatherAgent : Agent
{
    private static readonly Regex EmailPattern = new(@"^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,6}$", RegexOptions.IgnoreCase);
    private const string DefaultModelName = "gpt-4";

    public string DefaultModel => DefaultModelName;

    /// <summary>
    /// System Prompt - Defines the agent's behavior and capabilities
    /// This is the first message sent to the LLM to establish the context
    /// </summary>
    public override string GetSystemPrompt(Dictionary<string, object> context)
    {
        return "You are a helpful weather assistant that can check the weather and send emails.\n" +
               "You can:\n" +
               "1. Get the current weather for a location using get_weather()\n" +
               "2. Send emails about the weather using send_email()\n\n" +
               "When asked about the weather, always use get_weather() to get accurate information.\n" +
               "When asked to send an email about the weather, first get the weather then use send_email().\n" +
               "If there's an error getting the weather, explain the issue to the user.\n\n" +
               "Be concise and friendly in your responses.";
    }

    /// <summary>
    /// Weather Tool Function
    /// </summary>
    [FunctionSpec("Get the current weather in a given location")]
    public async Task<string> GetWeather(
        [Parameter("The city and state, e.g. San Francisco, CA")] string location,
        Dictionary<string, object> context)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("Location cannot be empty");
        }

        // Rate limiting
        const string rateLimitKey = "weather_last_call";
        if (context.TryGetValue(rateLimitKey, out var lastCallObj) && 
            lastCallObj is DateTime lastCall &&
            (DateTime.UtcNow - lastCall).TotalMilliseconds < 1000)
        {
            throw new InvalidOperationException("Rate limit exceeded. Please wait before making another request.");
        }
        context[rateLimitKey] = DateTime.UtcNow;

        try
        {
            // Simulate async API call
            await Task.Delay(100);

            // In a real implementation, you would call a weather API here
            var weather = new WeatherResponse(
                location,
                "72°F",
                "Sunny",
                "45%",
                "10 mph NW",
                "Clear skies for the next 24 hours"
            );

            return weather.ToString();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Failed to get weather for location: {location}", e);
        }
    }

    /// <summary>
    /// Email Tool Function
    /// </summary>
    [FunctionSpec("Send an email with the weather information")]
    public async Task<string> SendEmail(
        [Parameter("Email recipient")] string to,
        [Parameter("Email body")] string body,
        Dictionary<string, object> context,
        [Parameter("Email subject")] string subject = "Weather Update")
    {
        // Validate email format
        if (!EmailPattern.IsMatch(to))
        {
            throw new ArgumentException("Invalid email address format");
        }

        // Validate content
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Email body cannot be empty");
        }

        try
        {
            // Simulate async API call
            await Task.Delay(100);

            // In a real implementation, you would send an actual email here
            return $"Email sent to {to} with subject: {subject}";
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Failed to send email", e);
        }
    }

    /// <summary>
    /// Helper class for weather data
    /// </summary>
    private record WeatherResponse(
        string Location,
        string Temperature,
        string Conditions,
        string Humidity,
        string Wind,
        string Forecast)
    {
        public override string ToString() =>
            $"Current weather in {Location}:\n" +
            $"Temperature: {Temperature}\n" +
            $"Conditions: {Conditions}\n" +
            $"Humidity: {Humidity}\n" +
            $"Wind: {Wind}\n" +
            $"Forecast: {Forecast}";
    }
}
