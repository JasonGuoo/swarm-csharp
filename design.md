# Swarm C# Implementation Design Document

## Core Components

1. **Agent System**
   - Core interface: `IAgent`
   - Key responsibilities:
     - Execute requests with context
     - Provide system prompt
     - Define tool choice mode
     - Implement functions with attributes

2. **Function Attribute System**
   - `[FunctionSpec]`: Defines function metadata
     - Name: Function name for LLM
     - Description: Function description
   - `[Parameter]`: Defines parameter metadata
     - Name: Parameter name
     - Description: Parameter description
     - Type: Parameter type (default: "string")
     - Required: Whether parameter is required (default: true)

3. **Result System**
   - Core class: `Result<T>`
   - Encapsulates function return values:
     - Value: The actual return value
     - ContextUpdates: Updates to context variables
   - Supports:
     - Simple string returns
     - Complex object returns
     - Context variable updates

4. **LLM Client System**
   - Base interface: `ILLMClient`
   - Implementations:
     - OpenAIClient
     - AzureOpenAIClient
     - ChatGLMClient
     - OllamaClient
   - Configuration:
     - API keys
     - Base URLs
     - Model selection
     - Provider-specific settings

5. **Context Management**
   - Maintains shared context between turns
   - Supports variable persistence
   - Thread-safe operations
   - Async/await support

## Core Workflows

1. **Main Loop Flow**
```
Initialize
    - Create active agent
    - Initialize context variables
    - Create message history
    ↓
Start Turn Loop (while history.Count - initLen < maxTurns)
    ↓
Get LLM Response
    - Build request with agent, history, context
    - Get completion from LLM (streaming or non-streaming)
    - Check for response errors
    ↓
Process Response
    - If streaming:
        - Accumulate content chunks
        - Track tool call messages
        - Create final combined message
    - Add response to history
    ↓
Handle Tool Calls (if present)
    - Extract function name and arguments
    - Validate function exists
    - Parse and convert arguments
    - Execute function with context
    - Add tool call result to history
    - Update active agent if result is IAgent
    ↓
Update Context
    - Process context updates
    - Validate updates
    - Apply thread-safe updates
    ↓
Continue or Complete
    - If no tool calls: task complete
    - If max turns reached: complete
    - Otherwise: continue loop
```

2. **Function Discovery Flow**
```
Scan Agent Class
    ↓
Find [FunctionSpec] Methods
    ↓
For Each Method:
    - Get function name & description
    - Get parameter attributes
    - Build parameter schema
    - Create function mapping
    ↓
Build Function Descriptions
```

3. **Function Execution Flow**
```
Receive Tool Call
    ↓
Find Attributed Method
    ↓
Convert Parameters
    - JSON to C# types
    - Validate required params
    - Apply type conversion
    ↓
Invoke Method
    ↓
Process Result
    - Convert to JSON
    - Update context
```

4. **Result Processing Flow**
```
Function Execution
    ↓
Create Result
    - Wrap return value
    - Add context updates
    ↓
Process Result
    - Update context
    - Format for LLM
    - Handle special cases
    ↓
Return to Agent
```

## Example Implementation

1. **Weather Agent Example**
```csharp
public class WeatherAgent : IAgent
{
    [FunctionSpec(
        Name = "get_weather",
        Description = "Get weather for location"
    )]
    [Parameter(Name = "location", Description = "City and state")]
    public async Task<Result<WeatherResponse>> GetWeatherAsync(string location)
    {
        var weather = // get weather...
        return new Result<WeatherResponse>(weather)
            .WithContextUpdate("last_location", location)
            .WithContextUpdate("last_weather", weather);
    }
}
```

2. **Result Class Example**
```csharp
public class Result<T>
{
    public T Value { get; }
    private readonly Dictionary<string, object> _contextUpdates;
    
    public Result(T value)
    {
        Value = value;
        _contextUpdates = new Dictionary<string, object>();
    }
    
    public Result<T> WithContextUpdate(string key, object value)
    {
        _contextUpdates[key] = value;
        return this;
    }
}
```

## Key Features

1. **Attribute-Based Discovery**
   - No manual function registration
   - Automatic schema generation
   - Type-safe parameter handling
   - Async/await support

2. **Flexible LLM Support**
   - Multiple provider support
   - Environment-based configuration
   - Model configuration in client
   - HTTP/2 and gRPC support

3. **Simple Agent Development**
   - Implement IAgent interface
   - Add function attributes
   - Define system prompt
   - Full async support

4. **Error Handling**
   - LLM API errors
   - Function execution errors
   - Async error propagation
   - Retry policies

## C#-Specific Enhancements

1. **Async/Await First**
   - All operations are async by default
   - Task-based asynchronous pattern (TAP)
   - Cancellation support
   - IAsyncEnumerable for streaming

2. **Strong Typing**
   - Generic Result<T> type
   - System.Text.Json integration
   - Source generators for JSON
   - Type-safe context updates

3. **Modern C# Features**
   - Record types for immutable data
   - Pattern matching
   - Nullable reference types
   - Init-only properties

4. **Performance Optimizations**
   - Memory pooling
   - Span<T> for efficient memory
   - ValueTask for performance
   - Minimal allocations
