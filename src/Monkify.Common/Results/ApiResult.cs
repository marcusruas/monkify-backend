using Monkify.Common.Messaging;

namespace Monkify.Results
{
    public class ApiResult<T>
    {
        public ApiResult() { }

        public ApiResult(T data, IEnumerable<Message> messages)
        {
            Data = data;
            Messages = messages;
        }

        public T Data { get; set; }
        public IEnumerable<Message> Messages { get; set; }
    }
}
