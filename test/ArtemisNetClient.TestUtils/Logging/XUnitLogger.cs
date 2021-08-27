using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.TestUtils.Logging
{
    public sealed class XUnitLogger : ILogger
    {
        private static readonly string LoglevelPadding = ": ";
        private static readonly string MessagePadding;

        private readonly string _name;
        private readonly ITestOutputHelper _output;

        static XUnitLogger()
        {
            var logLevelString = GetLogLevelString(LogLevel.Information);
            MessagePadding = new string(' ', logLevelString.Length + LoglevelPadding.Length);
        }

        public XUnitLogger(ITestOutputHelper output, string name)
        {
            _output = output;
            _name = name;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);

            if (!string.IsNullOrEmpty(message) || exception != null)
            {
                WriteMessage(logLevel, _name, message, exception);
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        IDisposable ILogger.BeginScope<TState>(TState state)
        {
            throw new NotSupportedException();
        }

        private void WriteMessage(LogLevel logLevel, string logName, string message, Exception exception)
        {
            var logLevelString = GetLogLevelString(logLevel);

            _output.WriteLine($"{logLevelString}: {logName}");

            if (!string.IsNullOrEmpty(message))
            {
                _output.WriteLine($"{MessagePadding}{message}");
            }

            if (exception != null)
            {
                _output.WriteLine(exception.ToString());
            }
        }

        private static string GetLogLevelString(LogLevel logLevel) => logLevel switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            LogLevel.Critical => "crit",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
        };
    }
}