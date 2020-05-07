using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ActiveMQ.Artemis.Client.InternalUtilities
{
    internal class ConcurrentHashSet<T>
    {
        private readonly ConcurrentDictionary<T, byte> _dictionary = new ConcurrentDictionary<T, byte>();

        public void Add(T item)
        {
            _dictionary.TryAdd(item, byte.MinValue);
        }

        public void Remove(T item)
        {
            _dictionary.TryRemove(item, out _);
        }

        public IEnumerable<T> Values => _dictionary.Keys;
    }
}