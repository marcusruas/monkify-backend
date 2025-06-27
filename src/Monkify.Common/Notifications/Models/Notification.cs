using System;
using System.Collections.Generic;
using System.Text;

namespace Monkify.Common.Notifications
{
    public class Notification
    {
        public Notification(NotificationType type, string value)
        {
            Type = type;
            Value = value;
        }

        public NotificationType Type { get; }
        public string Value { get; }
    }
}
