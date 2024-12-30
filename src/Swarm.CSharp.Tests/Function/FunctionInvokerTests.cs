using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Swarm.CSharp.Function;
using Swarm.CSharp.Function.Attributes;
using Xunit;

namespace Swarm.CSharp.Tests.Function
{
    public class FunctionInvokerTests
    {
        private readonly FunctionInvoker _invoker;
        private readonly TestClass _testInstance;

        public FunctionInvokerTests()
        {
            _invoker = new FunctionInvoker();
            _testInstance = new TestClass();
        }

        [Fact]
        public void Invoke_SimpleMethod_ReturnsCorrectResult()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.Add));
            var parameters = new Dictionary<string, object>
            {
                ["a"] = 5,
                ["b"] = 3
            };

            // Act
            var result = _invoker.Invoke(method, _testInstance, parameters);

            // Assert
            Assert.Equal(8, result);
        }

        [Fact]
        public void Invoke_WithStringConversion_ReturnsCorrectResult()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.Add));
            var parameters = new Dictionary<string, object>
            {
                ["a"] = "5",
                ["b"] = "3"
            };

            // Act
            var result = _invoker.Invoke(method, _testInstance, parameters);

            // Assert
            Assert.Equal(8, result);
        }

        [Fact]
        public void Invoke_WithDefaultValue_UsesDefaultValue()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.AddWithDefault));
            var parameters = new Dictionary<string, object>
            {
                ["a"] = 5
            };

            // Act
            var result = _invoker.Invoke(method, _testInstance, parameters);

            // Assert
            Assert.Equal(15, result); // 5 + default(10)
        }

        [Fact]
        public void Invoke_WithoutRequiredParameter_ThrowsException()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.Add));
            var parameters = new Dictionary<string, object>
            {
                ["a"] = 5
                // Missing parameter b
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _invoker.Invoke(method, _testInstance, parameters));
        }

        [Fact]
        public void Invoke_WithInvalidType_ThrowsException()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.Add));
            var parameters = new Dictionary<string, object>
            {
                ["a"] = "not a number",
                ["b"] = 3
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _invoker.Invoke(method, _testInstance, parameters));
        }

        [Fact]
        public async Task InvokeAsync_AsyncMethod_ReturnsCorrectResult()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.AddAsync));
            var parameters = new Dictionary<string, object>
            {
                ["a"] = 5,
                ["b"] = 3
            };

            // Act
            var result = await _invoker.InvokeAsync(method, _testInstance, parameters);

            // Assert
            Assert.Equal(8, result);
        }

        [Fact]
        public void Invoke_WithoutFunctionSpec_ThrowsException()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithoutAttribute));
            var parameters = new Dictionary<string, object>();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _invoker.Invoke(method, _testInstance, parameters));
        }

        [Fact]
        public void Invoke_WithNullableParameter_HandlesNullCorrectly()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.ProcessNullable));
            var parameters = new Dictionary<string, object>
            {
                ["value"] = null
            };

            // Act
            var result = _invoker.Invoke(method, _testInstance, parameters);

            // Assert
            Assert.Equal("null", result);
        }

        private class TestClass
        {
            [FunctionSpec(Description = "Adds two numbers")]
            public int Add(
                [Parameter("First number")] int a,
                [Parameter("Second number")] int b)
            {
                return a + b;
            }

            [FunctionSpec(Description = "Adds two numbers with default")]
            public int AddWithDefault(
                [Parameter("First number")] int a,
                [Parameter("Second number", defaultValue: "10")] int b = 10)
            {
                return a + b;
            }

            [FunctionSpec(Description = "Adds two numbers asynchronously")]
            public async Task<int> AddAsync(
                [Parameter("First number")] int a,
                [Parameter("Second number")] int b)
            {
                await Task.Delay(1); // Simulate async work
                return a + b;
            }

            public void MethodWithoutAttribute()
            {
                // This method doesn't have FunctionSpec attribute
            }

            [FunctionSpec(Description = "Processes nullable value")]
            public string ProcessNullable(
                [Parameter("Nullable value")] string? value)
            {
                return value ?? "null";
            }
        }
    }
}
