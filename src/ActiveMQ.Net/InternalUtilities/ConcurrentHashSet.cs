using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ActiveMQ.Net.InternalUtilities
{
    internal class ConcurrentHashSet<T>
    {
        private readonly ConcurrentDictionary<T, T> _dictionary = new ConcurrentDictionary<T, T>();

        public void Add(T item)
        {
            _dictionary.TryAdd(item, item);
        }

        public void Remove(T item)
        {
            _dictionary.TryRemove(item, out _);
        }

        public IEnumerable<T> Values => _dictionary.Values;
    }
}