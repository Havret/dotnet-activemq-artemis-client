using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Amqp.Framing;
using Amqp.Listener;
using Amqp.Transactions;

namespace ActiveMQ.Artemis.Client.Testing;

public class TestKit : IDisposable
{
    private readonly ContainerHost _host;
    private readonly ConcurrentDictionary<string, MessageSource> _messageSources = new(StringComparer.InvariantCultureIgnoreCase);
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
        if (attach.Role)
        {
            return ((Source) attach.Source).Address;
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
            return;

        var address = message.To;

        if (_messageSources.TryGetValue(address, out var messageSource))
        {
            messageSource.Enqueue(message);
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

    private void OnMessageSource(string address, MessageSource messageSource)
    {
        var parsedAddress = GetAddress(address);
        _messageSources.TryAdd(parsedAddress, messageSource);
    }
    
    private string GetAddress(string address)
    {
        var fqqnMatch = Regex.Match(address, "(.+)::(.+)");
        return fqqnMatch.Success ? fqqnMatch.Groups[1].Value : address;
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
        var messageSource = await GetMessageSourceAsync(address);
        if (messageSource != null)
        {
            message.To = address;
            messageSource.Enqueue(message);
        }
        else
        {
            throw new InvalidOperationException($"No consumer registered on address {address}");
        }
    }

    private async Task<MessageSource?> GetMessageSourceAsync(string address)
    {
        int delay = 1;
        while (delay <= 10)
        {
            if (_messageSources.TryGetValue(address, out var messageSource))
            {
                return messageSource;
            }

            await Task.Delay(delay++);
        }

        return null;
    }

    public void Dispose()
    {
        _host.Close();
    }
}