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
        [Required(ErrorMessage = "Bet amount is required.")]
        public decimal? BetAmount { get; set; }
        [Required(ErrorMessage = "Bet choice is required.")]
        public string? BetChoice { get; set; }
    }
}
