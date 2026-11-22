using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client.Chaos;

/// <summary>
/// Writes the library's logs to a file, timestamped with the history clock so that they line up with the history.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly StreamWriter _writer;
    private readonly Func<double> _clock;
    private readonly LogLevel _minLevel;
    private readonly object _lock = new();

    public FileLoggerProvider(string path, Func<double> clock, LogLevel minLevel)
    {
        _writer = new StreamWriter(File.Create(path)) { AutoFlush = true };
        _clock = clock;
        _minLevel = minLevel;
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    private void Write(LogLevel level, string category, string message, Exception exception)
    {
        var line = $"{_clock():0.0000} {level,-11} {category}: {message}";
        if (exception != null)
        {
            line += Environment.NewLine + exception;
        }

        lock (_lock)
        {
            _writer.WriteLine(line);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer.Dispose();
        }
    }

    private sealed class FileLogger(FileLoggerProvider provider, string category) : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                provider.Write(logLevel, category, formatter(state, exception), exception);
            }
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel >= provider._minLevel;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
    }
}
