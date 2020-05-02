namespace ActiveMQ.Net
{
    internal sealed class Header
    {
        private readonly Amqp.Framing.Header _innerHeader;

        public Header(Amqp.Message innerMessage)
        {
            _innerHeader = innerMessage.Header ??= new Amqp.Framing.Header();
        }
        
        public bool? Durable
        {
            get => _innerHeader.HasField(0) ? _innerHeader.Durable : default(bool?);
            set
            {
                if (value != default)
                    _innerHeader.Durable = value.Value;
                else
                    _innerHeader.ResetField(0);
            }
        }

        public byte? Priority
        {
            get => _innerHeader.HasField(1) ? _innerHeader.Priority : default(byte?);
            set
            {
                if (value != default)
                    _innerHeader.Priority = value.Value;
                else
                    _innerHeader.ResetField(1);
            }
        }

        public uint? Ttl
        {
            get => _innerHeader.HasField(2) ? _innerHeader.Ttl : default(uint?);
            set
            {
                if (value != default)
                    _innerHeader.Ttl = value.Value;
                else
                    _innerHeader.ResetField(2);
            }            
        }
    }
}