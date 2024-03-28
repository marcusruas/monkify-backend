using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Monkey.ValueObjects
{
    public class SessionStatus
    {
        public SessionStatus(QueueStatus status)
        {
            Status = status;
        }

        public SessionStatus(string message)
        {
            Status = QueueStatus.ErrorOnStart;
            Message = message;
        }

        public QueueStatus Status { get; set; }
        public string? Message { get; set; }
    }
}
