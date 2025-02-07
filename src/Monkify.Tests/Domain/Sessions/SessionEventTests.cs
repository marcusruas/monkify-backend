using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Events;
using Monkify.Domain.Sessions.ValueObjects;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.Domain.Sessions
{
    public class SessionCreatedTests
    {
        [Fact]
        public void Constructor_InitializesPropertiesCorrectly()
        {
            SessionParameters sessionParameters = new SessionParameters
            {
                AllowedCharacters = SessionCharacterType.Letters,
                RequiredAmount = 100.0m,
                MinimumNumberOfPlayers = 5,
                ChoiceRequiredLength = 3,
                AcceptDuplicatedCharacters = false,
                Active = true,
                PresetChoices = new List<PresetChoice>
                {
                    new PresetChoice { Choice = "ABC" },
                    new PresetChoice { Choice = "XYZ" }
                }
            };

            var sessionId = Guid.NewGuid();
            var sessionCreated = new SessionCreated(sessionId, sessionParameters);

            sessionCreated.SessionId.ShouldBe(sessionId);
            sessionCreated.CharacterType.ShouldBe(sessionParameters.AllowedCharacters);
            sessionCreated.MinimumNumberOfPlayers.ShouldBe(sessionParameters.MinimumNumberOfPlayers);
            sessionCreated.ChoiceRequiredLength.ShouldBe(sessionParameters.ChoiceRequiredLength);
            sessionCreated.PresetChoices.ShouldNotBeNull();
            sessionCreated.PresetChoices.Count.ShouldBe(sessionParameters.PresetChoices.Count);
            sessionCreated.PresetChoices.ShouldBe(sessionParameters.PresetChoices.Select(x => x.Choice));
        }

        [Fact]
        public void Constructor_HandlesNullPresetChoices()
        {
            var sessionId = Guid.NewGuid();
            var parameters = new SessionParameters
            {
                AllowedCharacters = SessionCharacterType.Letters,
                MinimumNumberOfPlayers = 2,
                ChoiceRequiredLength = null,
                PresetChoices = null
            };

            var sessionCreated = new SessionCreated(sessionId, parameters);

            sessionCreated.PresetChoices.ShouldBeNull();
        }
    }
}
