using Monkify.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Sessions.Entities
{
    public class PresetChoice : TableEntity
    {
        public PresetChoice() { }
        public PresetChoice(string choice)
        {
            Choice = choice;
        }

        public SessionParameters Parameters { get; set; }
        public Guid ParametersId { get; set; }
        public string Choice { get; set; }
    }
}
