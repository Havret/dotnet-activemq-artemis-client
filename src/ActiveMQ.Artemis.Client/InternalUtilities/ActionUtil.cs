using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client.InternalUtilities
{
    internal static class ActionUtil
    {
        public static async Task ExecuteAll(params Func<Task>[] funcs)
        {
            var exceptions = new List<Exception>();
            foreach (var func in funcs)
            {
                try
                {
                    await func().ConfigureAwait(false);
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
    }
}