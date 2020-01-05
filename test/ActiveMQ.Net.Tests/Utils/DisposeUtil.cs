using System;
using System.Threading.Tasks;

namespace ActiveMQ.Net.Tests.Utils
{
    public static class DisposeUtil
    {
        public static async Task DisposeAll(params object[] disposables)
        {
            foreach (var obj in disposables)
            {
                try
                {
                    await TryDispose(obj);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        private static async Task TryDispose(object obj)
        {
            switch (obj)
            {
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync();
                    break;
            }
        }
    }
}