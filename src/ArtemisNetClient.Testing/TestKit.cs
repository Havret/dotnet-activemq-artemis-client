using System.Collections.Concurrent;
using ActiveMQ.Artemis.Client.Testing.Listener;
using Amqp.Framing;
using Amqp.Transactions;

namespace ActiveMQ.Artemis.Client.Testing;

public class TestKit : IDisposable
{
    private readonly ContainerHost _host;
    private readonly ConcurrentDictionary<string, List<SharedMessageSource>> _messageSources = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly Dictionary<string, List<Action<Message>>> _messageSubscriptions = new(StringComparer.InvariantCultureIgnoreCase);
    public TestKit(Endpoint endpoint)
    {
        _host = new ContainerHost(endpoint.Address);
        _host.AddressResolver = AddressResolver;
        _host.Listeners[0].HandlerFactory = _ => new Handler();
        _host.RegisterLinkProcessor(new TestLinkProcessor(OnMessage, OnMessageSource));
        _host.Open();
    }

    private string? AddressResolver(ContainerHost containerHost, Attach attach)
    {
        if (attach is { Role: true, Source: Source source })
        {
            return source.Address;
        }
        if (attach.Target is Target target)
        {
            return target.Address;
        }
        if (attach.Target is Coordinator)
        {
            return attach.LinkName;
        }

        return null;
    }

    private void OnMessage(Message message)
    {
        if (string.IsNullOrEmpty(message.To))
        {
            return;
        }

        var address = message.To;

        if (_messageSources.TryGetValue(address, out var messageSources))
        {
            lock (messageSources)
            {
                foreach (var messageQueue in messageSources)
                {
                    messageQueue.Enqueue(message);
                }
            }
        }

        lock (_messageSubscriptions)
        {
            if (_messageSubscriptions.TryGetValue(address, out var subscriptions))
            {
                foreach (var subscription in subscriptions)
                {
                    subscription(message);
                }
            }
        }
    }

    private void OnMessageSource(MessageSourceInfo info, MessageSource messageSource)
    {
        if (!_messageSources.TryGetValue(info.Address, out var messageSources))
        {
            messageSources = new List<SharedMessageSource>();
            _messageSources[info.Address] = messageSources;
        }

        lock (messageSources)
        {
            var existingMessageSource = messageSources.FirstOrDefault(x => x.Info == info);
            if (existingMessageSource != null)
            {
                existingMessageSource.AddMessageSource(messageSource);
            }
            else
            {
                messageSources.Add(new SharedMessageSource(info, messageSource));
            }
        }
    }

    public ISubscription Subscribe(string address)
    {
        var subscription = new Subscription();
        lock (_messageSubscriptions)
        {
            if (!_messageSubscriptions.TryGetValue(address, out var subscriptions))
            {
                subscriptions = new List<Action<Message>>();
                _messageSubscriptions.Add(address, subscriptions);
            }

            subscriptions.Add(subscription.OnMessage);
        }

        subscription.Disposed += (_, _) =>
        {
            lock (_messageSubscriptions)
            {
                if (_messageSubscriptions.TryGetValue(address, out var subscriptions))
                {
                    subscriptions.Remove(subscription.OnMessage);
                }
            }
        };
        return subscription;
    }

    public async Task SendMessageAsync(string address, Message message)
    {
        var messageSources = GetMessageSources(address);
        lock (messageSources)
        {
            if (messageSources.Count > 0)
            {
                foreach (var messageSource in messageSources)
                {
                    messageSource.Enqueue(message);
                }
            }
            else
            {
                throw new InvalidOperationException($"No consumer registered on address {address}");
            }
        }
    }

    private IReadOnlyList<SharedMessageSource> GetMessageSources(string address)
    {
        if (_messageSources.TryGetValue(address, out var messageSource))
        {
            return messageSource;
        }
        return Array.Empty<SharedMessageSource>();
    }

    public void Dispose()
    {
        _host.Close();
    }
}