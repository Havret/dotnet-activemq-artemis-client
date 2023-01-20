using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection.InternalUtils
{
    internal class AsyncValueLazy<T> where T : class
    {
        private readonly AsyncLock _mutex = new AsyncLock();
        private readonly Func<CancellationToken, Task<T>> _factory;
        private T _value;

        public AsyncValueLazy(Func<CancellationToken, Task<T>> factory)
        {
            _factory = factory;
        }
        
        public async ValueTask<T> GetValueAsync(CancellationToken cancellationToken)
        {
            if (_value != null)
            {
                return _value;
            }

            using (await _mutex.LockAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_value != null)
                {
                    return _value;
                }

                _value = await _factory(cancellationToken).ConfigureAwait(false);
                return _value;
            }
        }
    }
}