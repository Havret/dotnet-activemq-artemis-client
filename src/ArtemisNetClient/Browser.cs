using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client
{
    internal class Browser : IBrowser
    {

        private IConsumer _consumer;
        private Message _current;

        public Browser(IConsumer consumer)
        {
            _consumer = consumer;
        }

        public Message Current => _current;

        object IEnumerator.Current => _current;

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public async ValueTask DisposeAsync()
        {
            if (_consumer != null)
                await _consumer.DisposeAsync();
        }


        public IEnumerator<Message> GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            _current = Next();

            return _current != null;
        }

        private Message Next()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            var token = cts.Token;

            try
            {
                _current = null;
                _current = _consumer.ReceiveAsync(token).Result;

                return _current;
            }
            catch(OperationCanceledException ex)
            {
                var error = ex.Message;
                return null;
            }
            catch(Exception)
            {
                throw;
            }
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }
    }
}