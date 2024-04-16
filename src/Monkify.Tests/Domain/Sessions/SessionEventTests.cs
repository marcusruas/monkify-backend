using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Events;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Tests.Builders.StubBuilders;
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
            var builder = new SessionParametersStubBuilder();
            builder.AddPresetChoices(2);

            var sessionId = Guid.NewGuid();
            var sessionParameters = builder.BuildFirst();
            var sessionCreated = new SessionCreated(sessionId, sessionParameters);

            sessionCreated.SessionId.ShouldBe(sessionId);
            sessionCreated.CharacterType.ShouldBe(sessionParameters.SessionCharacterType);
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
                SessionCharacterType = SessionCharacterType.LowerCaseLetter,
                MinimumNumberOfPlayers = 2,
                ChoiceRequiredLength = null,
                PresetChoices = null
            };

            var sessionCreated = new SessionCreated(sessionId, parameters);

            sessionCreated.PresetChoices.ShouldBeNull();
        }
    }
}
