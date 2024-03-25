using System;
using System.Collections.Generic;
using System.Text;

namespace Monkify.Common.Messaging
{
    public class Message
    {
        public Message(string value)
        {
            Type = MessageType.Informational;
            Value = value;
        }

        public Message(MessageType type, string value)
        {
            Type = type;
            Value = value;
        }

        public MessageType Type { get; }
        public string Value { get; }
    }
}
