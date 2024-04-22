﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Sessions.ValueObjects
{
    public enum SessionCharacterType
    {
        [Description("0123456789")]
        Number = 1,
        [Description("abcdefghijklmnopqrstuvwxyz")]
        LowerCaseLetter,
        [Description("ABCDEFGHIJKLMNOPQRSTUVWXYZ")]
        UpperCaseLetter,
        [Description("abcdefghijklmnopqrstuvwxyz")]
        PresetLowercaseWords,
        [Description("ABCDEFGHIJKLMNOPQRSTUVWXYZ")]
        PresetUppercaseWords,
        PresetByPlayerChoices,
    }
}
