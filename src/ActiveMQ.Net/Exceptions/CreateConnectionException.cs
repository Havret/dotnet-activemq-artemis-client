using System;

namespace ActiveMQ.Net.Exceptions
{
    public class CreateConnectionException : Exception
    {
        internal CreateConnectionException(string message) : base(message)
        {
        }
    }
}