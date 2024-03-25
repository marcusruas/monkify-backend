using System;
using System.Collections.Generic;
using System.Text;

namespace Monkify.Common.Exceptions
{
    public class InternalErrorException : MessagingException
    {
        public InternalErrorException()
        {
            DefaultMessage = "Failed to process your request, please try again later";
        }

        public override int StatusCodeResult => 500;
        public override string DefaultMessage { get; }
    }
}
