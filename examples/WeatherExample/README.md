# Weather Agent Example

This example demonstrates how to create a simple weather agent using SwarmCSharp. The agent can fetch weather information for different locations using an external weather API.

## Features Demonstrated

- Basic agent implementation with system prompts
- Function attributes and parameter validation
- Integration with external weather API
- Error handling and response formatting
- Context management for conversation state

## Prerequisites

- .NET 8.0 SDK
- OpenAI API key or Azure OpenAI credentials
- Weather API key (from your preferred weather service provider)

## Project Structure

```
weather/
├── Program.cs              # Main entry point and example usage
├── WeatherAgent.cs         # Weather agent implementation
├── Models/
│   └── WeatherData.cs     # Weather data model
└── README.md              # This file
```

## Implementation Details

### WeatherAgent Class

The WeatherAgent class demonstrates:
- System prompt definition for weather-related tasks
- Function specifications for weather queries
- Parameter validation and type conversion
- External API integration

Key components:

```csharp
[SystemPrompt(@"You are a helpful weather assistant that can provide weather information...")]
public class WeatherAgent : Agent
{
    [FunctionSpec("Get current weather for a specific location")]
    public async Task<string> GetWeather(
        [Parameter("City name")] string city,
        [Parameter("Country code (optional)")] string? country = null)
    {
        // Implementation details...
    }
}
```

### Usage Example

```csharp
// Initialize the agent
var weatherAgent = new WeatherAgent(new OpenAIClient(apiKey: "your-api-key"));

// Example conversation
var response = await weatherAgent.Chat("What's the weather like in London?");
Console.WriteLine(response);

// Example with follow-up
response = await weatherAgent.Chat("How about Tokyo?");
Console.WriteLine(response);
```

## Configuration

1. Set up your API keys:

```bash
export OPENAI_API_KEY='your-openai-key'
export WEATHER_API_KEY='your-weather-api-key'
```

2. Or use appsettings.json:

```json
{
  "OpenAI": {
    "ApiKey": "your-openai-key"
  },
  "WeatherApi": {
    "ApiKey": "your-weather-api-key"
  }
}
```

## Running the Example

1. Clone the repository:
```bash
git clone https://github.com/JasonGuoo/swarm-csharp.git
```

2. Navigate to the weather example:
```bash
cd examples/weather
```

3. Run the example:
```bash
dotnet run
```

## Expected Output

```
User: What's the weather like in London?
Assistant: Let me check the current weather in London for you.
Current weather in London, UK:
- Temperature: 18°C
- Condition: Partly cloudy
- Humidity: 65%
- Wind: 12 km/h

User: How about Tokyo?
Assistant: I'll check the weather in Tokyo for you.
Current weather in Tokyo, Japan:
- Temperature: 25°C
- Condition: Clear
- Humidity: 70%
- Wind: 8 km/h
```

## Key Learning Points

1. **Agent Definition**
   - How to create a specialized agent with a focused system prompt
   - Defining function specifications with parameters

2. **Function Integration**
   - Using attributes to expose methods to the LLM
   - Parameter validation and type conversion
   - Error handling for API calls

3. **Context Management**
   - Maintaining conversation context for follow-up questions
   - Handling location references and defaults

4. **Best Practices**
   - Proper error handling and user feedback
   - Clear and concise function descriptions
   - Efficient API usage

## Extending the Example

You can extend this example by:
1. Adding support for weather forecasts
2. Including more weather data points
3. Implementing location validation
4. Adding temperature unit conversion
5. Supporting multiple weather data providers

## Troubleshooting

Common issues:
1. API key not set correctly
2. Rate limiting from weather API
3. Invalid location names
4. Network connectivity issues

## Additional Resources

- [SwarmCSharp Documentation](../../README.md)
- [OpenAI API Documentation](https://platform.openai.com/docs)
- [Weather API Documentation] (depends on your chosen provider) 