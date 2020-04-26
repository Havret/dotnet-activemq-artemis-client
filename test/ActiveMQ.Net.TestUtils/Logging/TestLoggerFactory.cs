using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ActiveMQ.Net.TestUtils.Logging
{
    public sealed class TestLoggerFactory : ILoggerFactory
    {
        private readonly ITestOutputHelper _output;

        public TestLoggerFactory(ITestOutputHelper output)
        {
            _output = output;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(_output, categoryName);
        }

        void IDisposable.Dispose()
        {
        }

        void ILoggerFactory.AddProvider(ILoggerProvider provider)
        {
        }
    }
}