using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Swarm.CSharp.Core.Exceptions;
using Swarm.CSharp.Function;
using Xunit;

namespace Swarm.CSharp.Tests.Function;

public class FunctionInvokerTests
{
    private readonly FunctionInvoker _invoker;
    private readonly TestClass _testClass;

    public FunctionInvokerTests()
    {
        var converterRegistry = TypeConverterRegistry.Default();
        var logger = new LoggerFactory().CreateLogger<FunctionInvoker>();
        _invoker = new FunctionInvoker(converterRegistry, logger);
        _testClass = new TestClass();
    }

    [Fact]
    public async Task InvokeAsync_SimpleMethod_ReturnsResult()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "x", 10 },
            { "y", 20 }
        };

        // Act
        var result = await _invoker.InvokeAsync(_testClass, "Add", parameters);

        // Assert
        Assert.Equal(30, result);
    }

    [Fact]
    public async Task InvokeAsync_AsyncMethod_ReturnsResult()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "message", "Hello" }
        };

        // Act
        var result = await _invoker.InvokeAsync(_testClass, "EchoAsync", parameters);

        // Assert
        Assert.Equal("Hello", result);
    }

    [Fact]
    public async Task InvokeAsync_WithTypeConversion_ConvertsParameters()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "number", "42" }, // String that should be converted to int
            { "flag", 1 }      // Int that should be converted to bool
        };

        // Act
        var result = await _invoker.InvokeAsync(_testClass, "ConvertTypes", parameters);

        // Assert
        Assert.Equal("42:True", result);
    }

    [Fact]
    public async Task InvokeAsync_WithDefaultParameters_UsesDefaults()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "required", "test" }
        };

        // Act
        var result = await _invoker.InvokeAsync(_testClass, "WithDefaults", parameters);

        // Assert
        Assert.Equal("test:default", result);
    }

    [Fact]
    public async Task InvokeAsync_WithComplexObject_ConvertsToObject()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "data", new Dictionary<string, object>
                {
                    { "Name", "Test" },
                    { "Value", 42 }
                }
            }
        };

        // Act
        var result = await _invoker.InvokeAsync(_testClass, "ProcessComplex", parameters);

        // Assert
        Assert.Equal("Test:42", result);
    }

    [Fact]
    public async Task InvokeAsync_MethodNotFound_ThrowsException()
    {
        // Arrange
        var parameters = new Dictionary<string, object>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SwarmException>(
            () => _invoker.InvokeAsync(_testClass, "NonExistentMethod", parameters));
        Assert.Equal("TOOL_NOT_FOUND", exception.ErrorCode);
    }

    [Fact]
    public async Task InvokeAsync_MissingRequiredParameter_ThrowsException()
    {
        // Arrange
        var parameters = new Dictionary<string, object>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SwarmException>(
            () => _invoker.InvokeAsync(_testClass, "Add", parameters));
        Assert.Equal("INVALID_ARGUMENTS", exception.ErrorCode);
    }

    [Fact]
    public async Task InvokeAsync_InvalidParameterType_ThrowsException()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "x", "not a number" },
            { "y", 20 }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SwarmException>(
            () => _invoker.InvokeAsync(_testClass, "Add", parameters));
        Assert.Equal("INVALID_ARGUMENTS", exception.ErrorCode);
    }
}

/// <summary>
/// Test class containing various methods for testing function invocation.
/// </summary>
public class TestClass
{
    public int Add(int x, int y) => x + y;

    public async Task<string> EchoAsync(string message)
    {
        await Task.Delay(10); // Simulate async work
        return message;
    }

    public string ConvertTypes(int number, bool flag)
    {
        return $"{number}:{flag}";
    }

    public string WithDefaults(string required, string optional = "default")
    {
        return $"{required}:{optional}";
    }

    public string ProcessComplex(ComplexData data)
    {
        return $"{data.Name}:{data.Value}";
    }
}

public class ComplexData
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
}
