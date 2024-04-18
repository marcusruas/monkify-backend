using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Handlers.Sessions.RegisterBet
{
    public class RegisterBetRequestBody
    {
        [Required(ErrorMessage = "Payment signature is required.")]
        public string? PaymentSignature { get; set; }
        [Required(ErrorMessage = "Wallet is required.")]
        public string? Wallet { get; set; }
        [Required(ErrorMessage = "Bet amount in number of tokens is required.")]
        public decimal? Amount { get; set; }
        [Required(ErrorMessage = "Bet choice is required.")]
        public string? Choice { get; set; }
    }
}
