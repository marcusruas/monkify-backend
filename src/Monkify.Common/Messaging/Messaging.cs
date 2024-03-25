using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Monkify.Common.Messaging
{
    public class Messaging : IMessaging
    {
        public Messaging()
        {
            _messages = new List<Message>();
        }

        private ICollection<Message> _messages = new List<Message>();
        public IEnumerable<Message> Messages => _messages;

        public void AddInformationalMessage(string message)
            => _messages.Add(new Message(MessageType.Informational, message));

        public bool HasInformational()
            => _messages.Any(x => x.Type == MessageType.Informational);

        public void AddAlertMessage(string message)
            => _messages.Add(new Message(MessageType.Alert, message));

        public bool HasAlerts()
            => _messages.Any(x => x.Type == MessageType.Alert);

        public void AddValidationFailureMessage(string message)
            => _messages.Add(new Message(MessageType.ValidationFailure, message));

        public bool HasValidationFailures()
            => _messages.Any(x => x.Type == MessageType.ValidationFailure);

        public void AddErrorMessage(string message)
            => _messages.Add(new Message(MessageType.Error, message));

        public bool HasErrors()
            => _messages.Any(x => x.Type == MessageType.Error);

        public void ReturnErrorMessage(string message)
        {
            AddErrorMessage(message);
            throw new InternalErrorException();
        }

        public void ReturnValidationFailureMessage(string message)
        {
            AddValidationFailureMessage(message);
            throw new ValidationFailureException();
        }
    }
}
