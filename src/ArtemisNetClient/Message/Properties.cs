﻿using System;

namespace ActiveMQ.Artemis.Client
{
    internal sealed class Properties
    {
        private readonly Amqp.Framing.Properties _innerProperties;

        internal Properties(Amqp.Message innerMessage)
        {
            _innerProperties = innerMessage.Properties ??= new Amqp.Framing.Properties();
        }

        public string MessageId
        {
            get => _innerProperties.MessageId;
            set
            {
                if (value != default)
                    _innerProperties.MessageId = value;
                else
                    _innerProperties.ResetField(0);
            }
        }

        public object ObjectMessageId
        {
            get => _innerProperties.GetMessageId();
            set
            {
                if (value is null)
                {
                    _innerProperties.ResetField(0);
                }
                else
                {
                    _innerProperties.SetMessageId(value);
                }
            }
        }

        public byte[] UserId
        {
            get => _innerProperties.UserId;
            set
            {
                if (value != default)
                    _innerProperties.UserId = value;
                else
                    _innerProperties.ResetField(1);
            }
        }

        public string Subject
        {
            get => _innerProperties.Subject;
            set
            {
                if (value != default)
                    _innerProperties.Subject = value;
                else
                    _innerProperties.ResetField(3);
            }
        }

        public string CorrelationId
        {
            get => _innerProperties.CorrelationId;
            set
            {
                if (value != default)
                    _innerProperties.CorrelationId = value;
                else
                    _innerProperties.ResetField(5);
            }
        }

        public object ObjectCorrelationId
        {
            get => _innerProperties.GetCorrelationId();
            set
            {
                if (value is null)
                {
                    _innerProperties.ResetField(5);
                }
                else
                {
                    _innerProperties.SetCorrelationId(value);
                }
            }
        }

        public string ContentType
        {
            get => _innerProperties.ContentType;
            set
            {
                if (value != default)
                    _innerProperties.ContentType = value;
                else
                    _innerProperties.ResetField(6);
            }
        }

        public string ContentEncoding
        {
            get => _innerProperties.ContentEncoding;
            set
            {
                if (value != default)
                    _innerProperties.ContentEncoding = value;
                else
                    _innerProperties.ResetField(7);
            }
        }

        public DateTime? AbsoluteExpiryTime
        {
            get => _innerProperties.HasField(8) ? _innerProperties.AbsoluteExpiryTime : default(DateTime?);
            set
            {
                if (value != default)
                    _innerProperties.AbsoluteExpiryTime = value.Value;
                else
                    _innerProperties.ResetField(8);
            }
        }

        public DateTime? CreationTime
        {
            get => _innerProperties.HasField(9) ? _innerProperties.CreationTime : default(DateTime?);
            set
            {
                if (value != default)
                    _innerProperties.CreationTime = value.Value;
                else
                    _innerProperties.ResetField(9);
            }
        }

        public string GroupId
        {
            get => _innerProperties.GroupId;
            set
            {
                if (value != default)
                    _innerProperties.GroupId = value;
                else
                    _innerProperties.ResetField(10);
            }
        }

        public uint? GroupSequence
        {
            get => _innerProperties.HasField(11) ? _innerProperties.GroupSequence : default(uint?);
            set
            {
                if (value != default)
                    _innerProperties.GroupSequence = value.Value;
                else
                    _innerProperties.ResetField(11);
            }
        }

        public string ReplyToGroupId
        {
            get => _innerProperties.ReplyToGroupId;
            set
            {
                if (value != default)
                    _innerProperties.ReplyToGroupId = value;
                else
                    _innerProperties.ResetField(12);
            }
        }

        public string To
        {
            get => _innerProperties.To;
            internal set
            {
                if (value != default)
                    _innerProperties.To = value;
                else
                    _innerProperties.ResetField(2);
            }
        }

        public string ReplyTo
        {
            get => _innerProperties.ReplyTo;
            internal set
            {
                if (value != default)
                    _innerProperties.ReplyTo = value;
                else
                    _innerProperties.ResetField(4);
            }
        }
    }
}