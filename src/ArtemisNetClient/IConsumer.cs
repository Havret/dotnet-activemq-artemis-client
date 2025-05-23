﻿using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Transactions;

namespace ActiveMQ.Artemis.Client
{
    public interface IConsumer : IAsyncDisposable
    {
        ValueTask<Message> ReceiveAsync(CancellationToken cancellationToken = default);
        ValueTask AcceptAsync(Message message, Transaction transaction, CancellationToken cancellationToken = default);
        void Modify(Message message, bool deliveryFailed, bool undeliverableHere);
        void Reject(Message message);
    }
}