using System;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Amqp.Sasl;
using Amqp.Types;

namespace ActiveMQ.Artemis.Client.UnitTests.Utils
{
    /// <summary>
    /// A raw AMQP peer that accepts a single connection, and closes it without an error as soon as the client
    /// tries to begin a session, without ever answering the begin. ContainerHost can't be used for that,
    /// as it answers the begin straight away.
    /// </summary>
    public sealed class CloseOnBeginPeer : IDisposable
    {
        private const byte AmqpFrameType = 0;
        private const byte SaslFrameType = 1;
        private const byte SmallUlongFormatCode = 0x53;
        private const byte BeginDescriptorCode = 0x11;

        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();

        public CloseOnBeginPeer(Endpoint endpoint)
        {
            // Dual mode, so it doesn't matter whether localhost resolves to IPv4 or IPv6
            _listener = TcpListener.Create(endpoint.Port);
            _listener.Start();
            Task.Run(() => RunAsync(_cts.Token));
        }

        /// <summary>
        /// Set when the close frame has been sent in response to the begin.
        /// </summary>
        public ManualResetEvent CloseSent { get; } = new(false);

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                _listener.Stop();
                await using var stream = client.GetStream();

                // SASL layer, accept any credentials
                await EchoProtocolHeader(stream, cancellationToken);
                await WriteFrame(stream, SaslFrameType, new SaslMechanisms { SaslServerMechanisms = [new Symbol("PLAIN"), new Symbol("ANONYMOUS")] }, cancellationToken);
                await ReadFrame(stream, cancellationToken); // sasl-init
                await WriteFrame(stream, SaslFrameType, new SaslOutcome { Code = SaslCode.Ok }, cancellationToken);

                // AMQP layer
                await EchoProtocolHeader(stream, cancellationToken);
                await ReadFrame(stream, cancellationToken); // open
                await WriteFrame(stream, AmqpFrameType, new Open { ContainerId = nameof(CloseOnBeginPeer) }, cancellationToken);

                while (!IsBegin(await ReadFrame(stream, cancellationToken)))
                {
                }

                // A clean close, the error is deliberately left out
                await WriteFrame(stream, AmqpFrameType, new Close(), cancellationToken);
                CloseSent.Set();

                // Wait for the client to answer the close and hang up
                while (await ReadFrame(stream, cancellationToken) != null)
                {
                }
            }
            catch (Exception)
            {
                // The peer is torn down by the test, or the client went away
            }
        }

        private static async Task EchoProtocolHeader(NetworkStream stream, CancellationToken cancellationToken)
        {
            var header = new byte[8];
            await stream.ReadExactlyAsync(header, cancellationToken);
            await stream.WriteAsync(header, cancellationToken);
        }

        /// <returns>The whole frame, or null when the client closed the socket.</returns>
        private static async Task<byte[]> ReadFrame(NetworkStream stream, CancellationToken cancellationToken)
        {
            var sizeBuffer = new byte[4];
            if (await stream.ReadAtLeastAsync(sizeBuffer, 4, throwOnEndOfStream: false, cancellationToken) < 4)
            {
                return null;
            }

            var frame = new byte[BinaryPrimitives.ReadUInt32BigEndian(sizeBuffer)];
            sizeBuffer.CopyTo(frame, 0);
            await stream.ReadExactlyAsync(frame.AsMemory(4), cancellationToken);
            return frame;
        }

        private static bool IsBegin(byte[] frame)
        {
            // The performative follows the frame header (doff is in 4-byte words) and starts with its descriptor
            var bodyOffset = frame[4] * 4;
            return frame.Length > bodyOffset + 2
                   && frame[bodyOffset] == 0x00
                   && frame[bodyOffset + 1] == SmallUlongFormatCode
                   && frame[bodyOffset + 2] == BeginDescriptorCode;
        }

        private static async Task WriteFrame(NetworkStream stream, byte frameType, DescribedList performative, CancellationToken cancellationToken)
        {
            var body = new ByteBuffer(128, true);
            Encoder.WriteObject(body, performative);

            var frame = new byte[8 + body.Length];
            BinaryPrimitives.WriteUInt32BigEndian(frame, (uint)frame.Length);
            frame[4] = 2; // doff
            frame[5] = frameType;
            Array.Copy(body.Buffer, body.Offset, frame, 8, body.Length);
            await stream.WriteAsync(frame, cancellationToken);
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
        }
    }
}
