using Monkify.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Monkify.Common.Notifications
{
    public class NotificationsService : INotifications
    {
        public NotificationsService()
        {
            _Notifications = new List<Notification>();
        }

        private ICollection<Notification> _Notifications = new List<Notification>();
        public IEnumerable<Notification> Notifications => _Notifications;

        public void AddInformationalNotification(string Notification)
            => _Notifications.Add(new Notification(NotificationType.Informational, Notification));

        public bool HasInformational()
            => _Notifications.Any(x => x.Type == NotificationType.Informational);

        public void AddAlertNotification(string Notification)
            => _Notifications.Add(new Notification(NotificationType.Alert, Notification));

        public bool HasAlerts()
            => _Notifications.Any(x => x.Type == NotificationType.Alert);

        public void AddValidationFailureNotification(string Notification)
            => _Notifications.Add(new Notification(NotificationType.ValidationFailure, Notification));

        public bool HasValidationFailures()
            => _Notifications.Any(x => x.Type == NotificationType.ValidationFailure);

        public void AddErrorNotification(string Notification)
            => _Notifications.Add(new Notification(NotificationType.Error, Notification));

        public bool HasErrors()
            => _Notifications.Any(x => x.Type == NotificationType.Error);

        public void ReturnErrorNotification(string Notification)
        {
            AddErrorNotification(Notification);
            throw new InternalErrorException();
        }

        public void ReturnValidationFailureNotification(string Notification)
        {
            AddValidationFailureNotification(Notification);
            throw new ValidationFailureException();
        }
    }
}
