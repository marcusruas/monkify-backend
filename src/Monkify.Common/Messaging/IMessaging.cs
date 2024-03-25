using System;
using System.Collections.Generic;
using System.Text;

namespace Monkify.Common.Messaging
{
    public interface IMessaging
    {
        /// <summary>
        /// Returns the recorded messages in an immutable object
        /// </summary>
        IEnumerable<Message> Messages { get; }
        /// <summary>
        /// Adds a message of type <see cref="MessageType.Informational"/>
        /// </summary>
        /// <param name="message">message text</param>
        void AddInformationalMessage(string message);
        /// <summary>
        /// Adds a message of type <see cref="MessageType.Alert"/>
        /// </summary>
        /// <param name="message">message text</param>
        void AddAlertMessage(string message);
        /// <summary>
        /// Adds a message of type <see cref="MessageType.ValidationFailure"/>
        /// </summary>
        /// <param name="message">message text</param>
        void AddValidationFailureMessage(string message);
        /// <summary>
        /// Adds a message of type <see cref="MessageType.Error"/>
        /// </summary>
        /// <param name="message">message text</param>
        void AddErrorMessage(string message);
        /// <summary>
        /// Validates whether among the recorded messages there is one whose MessageType is <see cref="MessageType.Informational"/>
        /// </summary>
        bool HasInformational();
        /// <summary>
        /// Validates whether among the recorded messages there is one whose MessageType is <see cref="MessageType.Alert"/>
        /// </summary>
        bool HasAlerts();
        /// <summary>
        /// Validates whether among the recorded messages there is one whose MessageType is <see cref="MessageType.ValidationFailure"/>
        /// </summary>
        bool HasValidationFailures();
        /// <summary>
        /// Validates whether among the recorded messages there is one whose MessageType is <see cref="MessageType.Error"/>
        /// </summary>
        bool HasErrors();
        /// <summary>
        /// Inserts into the recorded messages a message of type <see cref="MessageType.Error"/> and throws an exception of type <see cref="InternalErrorException"/>
        /// </summary>
        void ReturnErrorMessage(string message);
        /// <summary>
        /// Inserts into the recorded messages a message of type <see cref="MessageType.ValidationFailure"/> and throws an exception of type <see cref="ValidationFailureException"/>
        /// </summary>
        void ReturnValidationFailureMessage(string message);
    }
}
