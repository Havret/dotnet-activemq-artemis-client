using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client.Management
{
    internal class TopologyManager : ITopologyManager
    {
        private const string OperationSucceeded = "_AMQ_OperationSucceeded";
        private const string ResourceName = "_AMQ_ResourceName";
        private const string BrokerResourceName = "broker";
        private const string OperationName = "_AMQ_OperationName";
        private const string EmptyRequest = "[]";

        private readonly RpcClient _rpcClient;

        public TopologyManager(RpcClient rpcClient)
        {
            _rpcClient = rpcClient;
        }

        public async Task<IReadOnlyList<string>> GetAddressNamesAsync(CancellationToken cancellationToken)
        {
            var response = await SendAsync("getAddressNames", EmptyRequest, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<string[][]>(response).First();
        }

        public async Task<IReadOnlyList<string>> GetQueueNamesAsync(CancellationToken cancellationToken)
        {
            var response = await SendAsync("getQueueNames", EmptyRequest, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<string[][]>(response).First();
        }

        public Task CreateAddressAsync(string name, RoutingType routingType, CancellationToken cancellationToken)
        {
            return CreateAddressAsync(name, new[] { routingType }, cancellationToken);
        }

        public Task CreateAddressAsync(string name, IEnumerable<RoutingType> routingTypes, CancellationToken cancellationToken)
        {
            var requestJson = RequestSerializer.CreateAddressToJson(name, routingTypes);
            return SendAsync("createAddress", requestJson, cancellationToken);
        }

        public async Task CreateQueueAsync(QueueConfiguration configuration, CancellationToken cancellationToken = default)
        {
            var serialize = await RequestSerializer.CreateQueueToJson(configuration).ConfigureAwait(false);
            await SendAsync("createQueue", serialize, cancellationToken).ConfigureAwait(false);
        }

        private async Task<string> SendAsync(string operation, string request, CancellationToken cancellationToken)
        {
            var message = new Message(request);
            message.ApplicationProperties[ResourceName] = BrokerResourceName;
            message.ApplicationProperties[OperationName] = operation;
            var response = await _rpcClient.SendAsync(message, cancellationToken).ConfigureAwait(false);

            var payload = response.GetBody<string>();
            if (response.ApplicationProperties.TryGetValue<bool>(OperationSucceeded, out var operationSucceeded) && operationSucceeded)
            {
                return payload;
            }
            else
            {
                var error = JsonSerializer.Deserialize<string[]>(payload);
                throw new InvalidOperationException(error.First());
            }
        }

        public ValueTask DisposeAsync()
        {
            return _rpcClient.DisposeAsync();
        }
    }
}