using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client.InternalUtilities
{
    public static class DisposeUtil
    {
        public static async ValueTask DisposeAll(params object[] disposables)
        {
            var exceptions = new List<Exception>();
            foreach (var obj in disposables)
            {
                try
                {
                    await TryDispose(obj);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            if (exceptions.Any())
            {
                throw new AggregateException(exceptions);
            }
        }

        private static async ValueTask TryDispose(object obj)
        {
            switch (obj)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync();
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
    }
}