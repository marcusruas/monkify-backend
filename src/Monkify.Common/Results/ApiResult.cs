using Monkify.Common.Notifications;

namespace Monkify.Results
{
    public class ApiResult<T>
    {
        public ApiResult() { }

        public ApiResult(T data, IEnumerable<Notification> messages)
        {
            Data = data;
            Messages = messages;
        }

        public T Data { get; set; }
        public IEnumerable<Notification> Messages { get; set; }
    }
}
