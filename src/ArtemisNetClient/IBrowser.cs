using System;
using System.Collections.Generic;

namespace ActiveMQ.Artemis.Client
{
    public interface IBrowser : IEnumerator<Message>, IEnumerable<Message>, IAsyncDisposable
    {
    }
}