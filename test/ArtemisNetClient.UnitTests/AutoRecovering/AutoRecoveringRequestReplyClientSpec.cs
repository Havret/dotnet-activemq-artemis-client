using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.AutoRecovering;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.InternalUtilities;
using ActiveMQ.Artemis.Client.UnitTests.Utils;
using Amqp;
using Amqp.Framing;
using Amqp.Handler;
using Amqp.Listener;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.UnitTests.AutoRecovering
{
    public class AutoRecoveringRequestReplyClientSpec : ActiveMQNetSpec
    {
        public AutoRecoveringRequestReplyClientSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_not_recreate_disposed_request_reply_clients()
        {
            var endpoint = GetUniqueEndpoint();
            var linksAttached = new CountdownEvent(2);
            var connectionRecovered = new ManualResetEvent(false);
            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.LinkRemoteOpen when @event.Context is Attach:
                        linksAttached.Signal();
                        break;
                }
            });

            var host1 = CreateOpenedContainerHost(endpoint, testHandler);

            var connection = await CreateConnection(endpoint);
            connection.ConnectionRecovered += (_, _) => connectionRecovered.Set();
            var rpcClient = await connection.CreateRequestReplyClientAsync();

            Assert.True(linksAttached.Wait(Timeout));
            await rpcClient.DisposeAsync();

            linksAttached.Reset();

            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);

            var host2 = CreateOpenedContainerHost(endpoint, testHandler);

            Assert.True(connectionRecovered.WaitOne(Timeout));

            Assert.False(linksAttached.Wait(ShortTimeout));

            await DisposeUtil.DisposeAll(connection, host2);
        }

        [Fact]
        public async Task Should_send_request_once_connection_recovered_when_sent_during_outage()
        {
            var endpoint = GetUniqueEndpoint();
            var host1 = CreateOpenedContainerHost(endpoint);
            var connection = await CreateConnection(endpoint);
            var rpcClient = await connection.CreateRequestReplyClientAsync();
            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);

            using var cts = new CancellationTokenSource(Timeout);
            var sendTask = rpcClient.SendAsync("a1", new Message("foo"), cts.Token);

            // There is no broker, the request is expected to wait for the recovery rather than fail
            var completed = await Task.WhenAny(sendTask, Task.Delay(ShortTimeout));
            Assert.True(completed != sendTask, $"The request completed during the outage: {sendTask.Exception?.InnerException}");

            var requestReceived = new TaskCompletionSource<Amqp.Message>(TaskCreationOptions.RunContinuationsAsynchronously);
            var host2 = CreateOpenedHostRecordingRequests(endpoint, message => requestReceived.TrySetResult(message));

            var request = await requestReceived.Task.WaitAsync(Timeout);
            Assert.Equal("foo", request.Body);

            // Nobody is going to reply
            await cts.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sendTask);

            await DisposeUtil.DisposeAll(rpcClient, connection, host2);
        }

        [Fact]
        public async Task Should_fail_request_rather_than_send_it_again_when_connection_lost_while_waiting_for_reply()
        {
            var endpoint = GetUniqueEndpoint();
            var request1Received = new TaskCompletionSource<Amqp.Message>(TaskCreationOptions.RunContinuationsAsynchronously);
            var host1 = CreateOpenedHostRecordingRequests(endpoint, message => request1Received.TrySetResult(message));
            var connection = await CreateConnection(endpoint);
            var rpcClient = await connection.CreateRequestReplyClientAsync();

            using var cts = new CancellationTokenSource(Timeout);
            var sendTask = rpcClient.SendAsync("a1", new Message("foo"), cts.Token);

            // The broker has accepted the request, but the connection is lost before the reply arrives
            await request1Received.Task.WaitAsync(Timeout);
            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);

            // The request may have been processed, it's up to the caller to send it again
            await Assert.ThrowsAsync<RequestReplyClientClosedException>(() => sendTask.WaitAsync(Timeout));

            // The client is recovered along with the connection
            var request2Received = new TaskCompletionSource<Amqp.Message>(TaskCreationOptions.RunContinuationsAsynchronously);
            var host2 = CreateOpenedHostRecordingRequests(endpoint, message => request2Received.TrySetResult(message));
            var nextSendTask = rpcClient.SendAsync("a1", new Message("bar"), cts.Token);
            var request = await request2Received.Task.WaitAsync(Timeout);
            Assert.Equal("bar", request.Body);

            // Nobody is going to reply
            await cts.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => nextSendTask);

            await DisposeUtil.DisposeAll(rpcClient, connection, host2);
        }

        [Fact]
        public async Task Should_dispose_request_reply_client_when_connection_lost_at_the_same_time()
        {
            var endpoint = GetUniqueEndpoint();
            var host = CreateOpenedContainerHost(endpoint);
            await using var connection = await CreateConnection(endpoint);
            var rpcClient = await connection.CreateRequestReplyClientAsync();

            // Don't wait for the client to notice, so that the connection is lost while the links are being closed
            host.Dispose();

            await rpcClient.DisposeAsync();
        }

        [Fact]
        public async Task Should_fail_pending_request_when_request_reply_client_disposed()
        {
            var endpoint = GetUniqueEndpoint();
            var requestReceived = new TaskCompletionSource<Amqp.Message>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var host = CreateOpenedHostRecordingRequests(endpoint, message => requestReceived.TrySetResult(message));
            await using var connection = await CreateConnection(endpoint);
            var rpcClient = await connection.CreateRequestReplyClientAsync();

            var sendTask = rpcClient.SendAsync("a1", new Message("foo"), CancellationToken);
            await requestReceived.Task.WaitAsync(Timeout);

            await rpcClient.DisposeAsync();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => sendTask);
        }

        [Fact]
        public async Task Should_terminate_request_reply_client_when_link_closed_with_unauthorized_access()
        {
            // The client replies to the broker's detach only after it has processed it
            var linkDetached = new ManualResetEvent(false);
            var testHandler = new TestHandler(@event =>
            {
                if (@event.Id == EventId.LinkRemoteClose)
                {
                    linkDetached.Set();
                }
            });
            var endpoint = GetUniqueEndpoint();
            ListenerLink requestLink = null;
            using var host = CreateContainerHost(endpoint, testHandler);
            host.CreateTestLinkProcessor().SetHandler(context =>
            {
                // The link the client sends its requests over
                if (!context.Attach.Role)
                {
                    requestLink = context.Link;
                }

                return false;
            });
            host.Open();
            await using var connection = await CreateConnection(endpoint);
            var rpcClient = await connection.CreateRequestReplyClientAsync();

            try
            {
                await requestLink.CloseAsync(Timeout, new Error(ErrorCode.UnauthorizedAccess) { Description = "Unauthorized" });
            }
            catch (Exception)
            {
                // CloseAsync throws without waiting for the client
            }

            Assert.True(linkDetached.WaitOne(Timeout));

            // Retrying won't help, the request must fail rather than wait for a recovery that is never going to succeed
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var exception = await Assert.ThrowsAsync<RequestReplyClientClosedException>(() => rpcClient.SendAsync("a1", new Message("foo"), cts.Token));
            Assert.Equal(ErrorCode.UnauthorizedAccess, exception.ErrorCode);
        }

        [Fact]
        public async Task Should_not_suspend_request_reply_client_when_request_fails_on_client_already_replaced()
        {
            var endpoint = GetUniqueEndpoint();
            var requestCount = 0;
            var request1Received = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var request2Received = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var host = CreateOpenedHostRecordingRequests(endpoint, _ =>
            {
                var request = Interlocked.Increment(ref requestCount) == 1 ? request1Received : request2Received;
                request.TrySetResult(true);
            });
            await using var connection = await CreateConnectionWithoutAutomaticRecovery(endpoint);
            await using var rpcClient = new AutoRecoveringRequestReplyClient(NullLoggerFactory.Instance, new RequestReplyClientConfiguration());
            var recoveryRequested = false;
            rpcClient.RecoveryRequested += () => recoveryRequested = true;
            await rpcClient.RecoverAsync(connection, CancellationToken);

            using var cts = new CancellationTokenSource(Timeout);
            var sendTask = rpcClient.SendAsync("a1", new Message("foo"), cts.Token);
            await request1Received.Task.WaitAsync(Timeout);

            // Replacing the client disposes the old one, which fails the request pending on it. The failure comes
            // late, the client has already been replaced.
            await rpcClient.RecoverAsync(connection, CancellationToken);

            // The request was sent, so it fails, but the replacement is left as it is
            await Assert.ThrowsAsync<RequestReplyClientClosedException>(() => sendTask.WaitAsync(Timeout));
            Assert.False(rpcClient.IsSuspended);
            Assert.False(recoveryRequested);

            // And it's used for the next requests
            var nextSendTask = rpcClient.SendAsync("a1", new Message("bar"), cts.Token);
            await request2Received.Task.WaitAsync(Timeout);

            // Nobody is going to reply
            await cts.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => nextSendTask);
        }

        [Fact]
        public async Task Should_not_terminate_request_reply_client_when_request_fails_with_terminal_error_on_client_already_replaced()
        {
            var client1 = new FakeRequestReplyClient();
            var client2 = new FakeRequestReplyClient();
            var connection = new FakeConnection(client1, client2);
            await using var rpcClient = new AutoRecoveringRequestReplyClient(NullLoggerFactory.Instance, new RequestReplyClientConfiguration());
            await rpcClient.RecoverAsync(connection, CancellationToken);

            var sendTask = rpcClient.SendAsync("a1", new Message("foo"), CancellationToken);
            await client1.RequestSent.Task.WaitAsync(Timeout);
            await rpcClient.RecoverAsync(connection, CancellationToken);

            // The request fails late on the replaced client, before it was sent
            client1.PendingRequest.SetException(new RequestReplyClientClosedException("Unauthorized", ErrorCode.UnauthorizedAccess, null));

            // It's sent over the replacement, which is left as it is
            await client2.RequestSent.Task.WaitAsync(Timeout);
            var reply = new Message("bar");
            client2.PendingRequest.SetResult(reply);
            Assert.Same(reply, await sendTask.WaitAsync(Timeout));
            Assert.False(client2.IsDisposed);
        }

        [Fact]
        public async Task Should_not_terminate_request_reply_client_when_sent_request_fails_with_terminal_error_on_client_already_replaced()
        {
            var client1 = new FakeRequestReplyClient();
            var client2 = new FakeRequestReplyClient();
            var connection = new FakeConnection(client1, client2);
            await using var rpcClient = new AutoRecoveringRequestReplyClient(NullLoggerFactory.Instance, new RequestReplyClientConfiguration());
            await rpcClient.RecoverAsync(connection, CancellationToken);

            var sendTask = rpcClient.SendAsync("a1", new Message("foo"), CancellationToken);
            await client1.RequestSent.Task.WaitAsync(Timeout);
            await rpcClient.RecoverAsync(connection, CancellationToken);

            // The request sent over the replaced client fails late
            client1.PendingRequest.SetException(new RequestReplyClientClosedException("Unauthorized", ErrorCode.UnauthorizedAccess, null) { RequestSent = true });

            // It may have been processed, so it fails rather than being sent again
            await Assert.ThrowsAsync<RequestReplyClientClosedException>(() => sendTask.WaitAsync(Timeout));
            Assert.False(client2.RequestSent.Task.IsCompleted);

            // The replacement is left as it is and used for the next requests
            var nextSendTask = rpcClient.SendAsync("a1", new Message("bar"), CancellationToken);
            var reply = new Message("baz");
            client2.PendingRequest.SetResult(reply);
            Assert.Same(reply, await nextSendTask.WaitAsync(Timeout));
            Assert.False(client2.IsDisposed);
        }

        [Theory]
        [InlineData(ErrorCode.InternalError)]
        [InlineData(ErrorCode.NotFound)]
        public async Task Should_recreate_request_reply_client_when_its_request_link_closed_while_connection_stays_open(string errorCode)
        {
            using var host = new LinkTrackingHost();
            await using var connection = await CreateConnection(host.Endpoint);
            await using var rpcClient = await connection.CreateRequestReplyClientAsync();

            await host.CloseLink(host.RequestLinks.First(), errorCode);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var sendTask = rpcClient.SendAsync("a1", new Message("foo"), cts.Token);

            // The request is sent over the request link of a new client
            var request = await host.RequestReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("foo", request.Body);
            Assert.Equal(2, host.RequestLinks.Count);

            // Nobody is going to reply
            await cts.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sendTask);
        }

        [Fact]
        public async Task Should_fail_request_and_recreate_request_reply_client_when_its_reply_link_closed_while_waiting_for_reply()
        {
            var requestCount = 0;
            var request2Received = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var host = new LinkTrackingHost(() =>
            {
                if (Interlocked.Increment(ref requestCount) == 2)
                {
                    request2Received.TrySetResult(true);
                }
            });
            await using var connection = await CreateConnection(host.Endpoint);
            await using var rpcClient = await connection.CreateRequestReplyClientAsync();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var sendTask = rpcClient.SendAsync("a1", new Message("foo"), cts.Token);
            await host.RequestReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // The reply can't arrive anymore
            await host.CloseLink(host.ReplyLinks.First());

            // The request may have been processed, it's up to the caller to send it again
            await Assert.ThrowsAsync<RequestReplyClientClosedException>(() => sendTask.WaitAsync(TimeSpan.FromSeconds(10)));

            // The next request goes over a new client
            var nextSendTask = rpcClient.SendAsync("a1", new Message("bar"), cts.Token);
            await request2Received.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(2, host.RequestLinks.Count);
            Assert.Equal(2, host.ReplyLinks.Count);

            // Nobody is going to reply
            await cts.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => nextSendTask);
        }

        [Fact]
        public async Task Should_not_suspend_request_reply_client_once_disposed()
        {
            var rpcClient = new AutoRecoveringRequestReplyClient(NullLoggerFactory.Instance, new RequestReplyClientConfiguration());
            await rpcClient.RecoverAsync(new FakeConnection(new FakeRequestReplyClient()), CancellationToken);
            await rpcClient.DisposeAsync();

            // E.g. a request handling a failure at the same time, the requests waiting for the recovery would never be woken up
            rpcClient.Suspend();

            Assert.False(rpcClient.IsSuspended);
        }

        /// <summary>
        /// A host that keeps track of the links of request-reply clients, recording the requests and never replying.
        /// </summary>
        private sealed class LinkTrackingHost : IDisposable
        {
            private readonly TestContainerHost _host;

            // The client replies to the broker's detach only after it has processed it
            private readonly AutoResetEvent _linkDetached = new(false);

            public LinkTrackingHost(Action onRequest = null)
            {
                var testHandler = new TestHandler(@event =>
                {
                    if (@event.Id == EventId.LinkRemoteClose)
                    {
                        _linkDetached.Set();
                    }
                });
                _host = CreateContainerHost(handler: testHandler);
                _host.CreateTestLinkProcessor().SetHandler(context =>
                {
                    if (context.Attach.Role)
                    {
                        ReplyLinks.Enqueue(context.Link);
                        return false;
                    }

                    RequestLinks.Enqueue(context.Link);
                    context.Complete(new RecordingLinkEndpoint(message =>
                    {
                        RequestReceived.TrySetResult(message);
                        onRequest?.Invoke();
                    }), 30);
                    return true;
                });
                _host.Open();
            }

            public Endpoint Endpoint => _host.Endpoint;
            public ConcurrentQueue<ListenerLink> RequestLinks { get; } = new();
            public ConcurrentQueue<ListenerLink> ReplyLinks { get; } = new();
            public TaskCompletionSource<Amqp.Message> RequestReceived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

            /// <summary>
            /// Closes the link with a recoverable error and waits until the client has processed it.
            /// </summary>
            public async Task CloseLink(ListenerLink link, string errorCode = ErrorCode.InternalError)
            {
                try
                {
                    await link.CloseAsync(Timeout, new Error(errorCode) { Description = "Closed by the broker" });
                }
                catch (Exception)
                {
                    // CloseAsync throws without waiting for the client
                }

                Assert.True(_linkDetached.WaitOne(Timeout));
            }

            public void Dispose() => _host.Dispose();
        }

        /// <summary>
        /// Opens a host that records the messages sent over the client's sender links, accepting them and never replying.
        /// </summary>
        private static TestContainerHost CreateOpenedHostRecordingRequests(Endpoint endpoint, Action<Amqp.Message> onRequest)
        {
            // Set up before opening, so that the client can't attach to anything else
            var host = CreateContainerHost(endpoint);
            host.CreateTestLinkProcessor().SetHandler(context =>
            {
                if (context.Attach.Role)
                {
                    return false;
                }

                context.Complete(new RecordingLinkEndpoint(onRequest), 30);
                return true;
            });
            host.Open();
            return host;
        }

        private class RecordingLinkEndpoint(Action<Amqp.Message> onMessage) : LinkEndpoint
        {
            public override void OnMessage(MessageContext messageContext)
            {
                onMessage(messageContext.Message);
                messageContext.Complete();
            }

            public override void OnFlow(FlowContext flowContext)
            {
            }

            public override void OnDisposition(DispositionContext dispositionContext)
            {
                dispositionContext.Complete();
            }
        }
    }
}
