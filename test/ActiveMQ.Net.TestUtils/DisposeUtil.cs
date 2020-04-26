using System;
using System.Threading.Tasks;

namespace ActiveMQ.Net.TestUtils
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
                    // ignore
                }
            }
        }

        private static async Task TryDispose(object obj)
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