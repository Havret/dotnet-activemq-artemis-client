using System.Collections.Generic;
using System.Linq;

namespace ActiveMQ.Artemis.Client
{
    public sealed class ApplicationProperties
    {
        private readonly Amqp.Framing.ApplicationProperties _innerProperties;

        internal ApplicationProperties(Amqp.Message innerMessage)
        {
            _innerProperties = innerMessage.ApplicationProperties ??= new Amqp.Framing.ApplicationProperties();
        }

        public object this[string key]
        {
            get => _innerProperties.Map[key];
            set => _innerProperties.Map[key] = value;
        }

        /// <summary>
        /// Determines whether the <see cref="ActiveMQ.Artemis.Client.ApplicationProperties"></see> contains the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="ActiveMQ.Artemis.Client.ApplicationProperties"></see>.</param>
        /// <returns>true if the <see cref="ActiveMQ.Artemis.Client.ApplicationProperties"></see> contains a property with the specified key; otherwise, false.</returns>
        public bool ContainsKey(string key) => _innerProperties.Map.ContainsKey(key);

        /// <summary>
        /// Gets the property associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the property to get.</param>
        /// <param name="value">When this method returns, contains the property associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
        /// <returns>true if the <see cref="ActiveMQ.Artemis.Client.ApplicationProperties"></see> contains a property with the specified key; otherwise, false.</returns>
        public bool TryGetValue<T>(string key, out T value)
        {
            if (_innerProperties.Map.TryGetValue(key, out var objectValue) && objectValue is T typedValue)
            {
                value = typedValue;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// Gets the sequence containing the keys in the <see cref="ActiveMQ.Artemis.Client.ApplicationProperties">.</see>
        /// </summary>
        public IEnumerable<string> Keys => _innerProperties.Map.Keys.OfType<string>();
    }
}