namespace Monkify.Common.Exceptions
{
    public class ValidationFailureException : MessagingException
    {
        public ValidationFailureException()
        {
            DefaultMessage = "One or more of the requested pieces of information are invalid. Contact Support for more information.";
        }

        public override int StatusCodeResult => 400;
        public override string DefaultMessage { get; }
    }
}
