﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Monkey.ValueObjects
{
    public record SessionEndResult(int NumberOfWinners, string FirstChoiceTyped);
}
