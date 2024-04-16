using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.Builders.StubBuilders
{
    public class SessionParametersStubBuilder : StubBuilder<SessionParameters>
    {
        public override void RulesForObject()
        {
            Object
                .RuleFor(x => x.SessionCharacterType, x => x.PickRandom<SessionCharacterType>())
                .RuleFor(x => x.RequiredAmount, x => x.Random.Decimal(1, 10))
                .RuleFor(x => x.MinimumNumberOfPlayers, x => x.Random.Int(2, 500))
                .RuleFor(x => x.ChoiceRequiredLength, x => x.Random.Int(2, 40))
                .RuleFor(x => x.AcceptDuplicatedCharacters, x => x.Random.Bool())
                .RuleFor(x => x.Active, true);
        }

        public void AddPresetChoices(int numberOfChoices)
        {
            var choices = new List<PresetChoice>();

            for (int i = 0; i < numberOfChoices; i++)
                choices.Add(new PresetChoice() { Choice = Faker.Random.String2(4) });

            Object.RuleFor(x => x.PresetChoices, choices);
        }
    }
}
