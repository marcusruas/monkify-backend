using System;
using System.Collections.Generic;
using System.Text;

namespace Monkify.Common.Notifications
{
    public interface INotifications
    {
        /// <summary>
        /// Returns the recorded Notifications in an immutable object
        /// </summary>
        IEnumerable<Notification> Notifications { get; }
        /// <summary>
        /// Adds a Notification of type <see cref="NotificationType.Informational"/>
        /// </summary>
        /// <param name="notification">Notification text</param>
        void AddInformationalNotification(string notification);
        /// <summary>
        /// Adds a Notification of type <see cref="NotificationType.Alert"/>
        /// </summary>
        /// <param name="notification">Notification text</param>
        void AddAlertNotification(string notification);
        /// <summary>
        /// Adds a Notification of type <see cref="NotificationType.ValidationFailure"/>
        /// </summary>
        /// <param name="notification">Notification text</param>
        void AddValidationFailureNotification(string notification);
        /// <summary>
        /// Adds a Notification of type <see cref="NotificationType.Error"/>
        /// </summary>
        /// <param name="notification">Notification text</param>
        void AddErrorNotification(string notification);
        /// <summary>
        /// Validates whether among the recorded Notifications there is one whose NotificationType is <see cref="NotificationType.Informational"/>
        /// </summary>
        bool HasInformational();
        /// <summary>
        /// Validates whether among the recorded Notifications there is one whose NotificationType is <see cref="NotificationType.Alert"/>
        /// </summary>
        bool HasAlerts();
        /// <summary>
        /// Validates whether among the recorded Notifications there is one whose NotificationType is <see cref="NotificationType.ValidationFailure"/>
        /// </summary>
        bool HasValidationFailures();
        /// <summary>
        /// Validates whether among the recorded Notifications there is one whose NotificationType is <see cref="NotificationType.Error"/>
        /// </summary>
        bool HasErrors();
        /// <summary>
        /// Inserts into the recorded Notifications a Notification of type <see cref="NotificationType.Error"/> and throws an exception of type <see cref="InternalErrorException"/>
        /// </summary>
        void ReturnErrorNotification(string notification);
        /// <summary>
        /// Inserts into the recorded Notifications a Notification of type <see cref="NotificationType.ValidationFailure"/> and throws an exception of type <see cref="ValidationFailureException"/>
        /// </summary>
        void ReturnValidationFailureNotification(string notification);
    }
}
