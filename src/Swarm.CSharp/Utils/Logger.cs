using Microsoft.Extensions.Logging;

namespace Swarm.CSharp.Utils
{
    /// <summary>
    /// A static logging utility that provides both console and file logging capabilities.
    /// The logger writes to both console and a log file simultaneously.
    /// Ref: https://learn.microsoft.com/en-us/answers/questions/1377949/logging-in-c-to-a-text-file
    /// 
    /// Features:
    /// - Console output with timestamps
    /// - File logging with detailed timestamps
    /// - Support for different log levels (Debug, Information, Warning, Error)
    /// - Exception logging with stack traces
    /// - Single-line console formatting
    /// - Automatic log directory creation
    /// - New log file for each application run with timestamp in filename
    /// 
    /// Log File Format:
    /// [yyyy-MM-dd HH:mm:ss] [LogLevel] [CategoryName] Message
    /// 
    /// Console Format:
    /// HH:mm:ss [LogLevel] Message
    /// 
    /// File Location:
    /// - Logs are written to "logs/swarm-{timestamp}.log"
    /// - Example: logs/swarm-20240101_123456.log
    /// - The logs directory is automatically created if it doesn't exist
    /// - Each application run creates a new log file
    /// 
    /// Log Levels:
    /// - Debug: Detailed information for debugging
    /// - Information: General information about application flow
    /// - Warning: Warnings that don't prevent the application from working
    /// - Error: Error conditions and exceptions
    /// </summary>
    /// <example>
    /// Basic usage:
    /// <code>
    /// // Log different levels of messages
    /// Logger.LogDebug("Debug level message");
    /// Logger.LogInformation("Information level message");
    /// Logger.LogWarning("Warning level message");
    /// Logger.LogError("Error level message");
    /// 
    /// // Log an exception with message
    /// try 
    /// {
    ///     // Some code that might throw
    ///     throw new Exception("Something went wrong");
    /// }
    /// catch (Exception ex)
    /// {
    ///     Logger.LogError(ex, "An error occurred while processing");
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// The logger automatically creates a new log file each time the application starts,
    /// using the current timestamp in the filename. This prevents log files from growing
    /// too large and makes it easier to find logs for specific application runs.
    /// 
    /// The first log entry in each file will indicate the log file path that was created.
    /// </remarks>
    public static class Logger
    {
        private static readonly ILogger _logger;
        private static readonly StreamWriter _logFileWriter;

        static Logger()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string logFilePath = $"logs/swarm-{timestamp}.log";
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
            _logFileWriter = new StreamWriter(logFilePath, append: false);

            var factory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Debug)
                    .AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.SingleLine = true;
                        options.TimestampFormat = "HH:mm:ss ";
                    })
                    .AddProvider(new CustomFileLoggerProvider(_logFileWriter));
            });

            _logger = factory.CreateLogger("Swarm.CSharp");
            LogInformation($"Started new log file: {logFilePath}");
        }

        public static void LogDebug(string message) => _logger.LogDebug(message);
        public static void LogInformation(string message) => _logger.LogInformation(message);
        public static void LogWarning(string message) => _logger.LogWarning(message);
        public static void LogError(string message) => _logger.LogError(message);
        public static void LogError(Exception ex, string message) => _logger.LogError(ex, message);
    }

    /// <summary>
    /// Custom logger provider that manages the creation and lifecycle of file loggers.
    /// This provider is responsible for creating logger instances and managing the log file stream.
    /// </summary>
    internal class CustomFileLoggerProvider : ILoggerProvider
    {
        private readonly StreamWriter _logFileWriter;

        public CustomFileLoggerProvider(StreamWriter logFileWriter)
        {
            _logFileWriter = logFileWriter ?? throw new ArgumentNullException(nameof(logFileWriter));
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new CustomFileLogger(categoryName, _logFileWriter);
        }

        public void Dispose()
        {
            _logFileWriter.Dispose();
        }
    }

    /// <summary>
    /// Custom logger implementation that handles the actual writing of log messages to a file.
    /// This logger formats messages with timestamps and log levels before writing to the file.
    /// 
    /// Log Format:
    /// [yyyy-MM-dd HH:mm:ss] [LogLevel] [CategoryName] Message
    /// Exception: {Exception details if present}
    /// </summary>
    internal class CustomFileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly StreamWriter _logFileWriter;

        public CustomFileLogger(string categoryName, StreamWriter logFileWriter)
        {
            _categoryName = categoryName;
            _logFileWriter = logFileWriter;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel.Debug;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _logFileWriter.WriteLine($"[{timestamp}] [{logLevel}] [{_categoryName}] {message}");
            if (exception != null)
            {
                _logFileWriter.WriteLine($"Exception: {exception}");
            }
            _logFileWriter.Flush();
        }
    }
}