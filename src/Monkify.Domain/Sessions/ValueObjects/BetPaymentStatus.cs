﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Sessions.ValueObjects
{
    public enum BetPaymentStatus
    {
        NotApplicable,
        NeedsRewarding,
        Rewarded,
        NeedsRefunding,
        Refunded,
        NeedsManualAnalysis
    }
}