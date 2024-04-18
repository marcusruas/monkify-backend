using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Common.Results
{
    public class ValidationResult
    {
        public ValidationResult()
        {
            IsValid = true;
        }

        public ValidationResult(string errorMessage)
        {
            IsValid = false;
            ErrorMessage = errorMessage;
        }

        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
