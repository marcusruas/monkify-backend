﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Sessions.Events
{
    public record BetCreated(string Wallet, string PaymentSignature, decimal Amount, string Choice) { }
}
