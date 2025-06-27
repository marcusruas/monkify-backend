using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using Monkify.Domain.Sessions.Entities;
using Monkify.Infrastructure.Handlers.Sessions.RegisterBet;

namespace Monkify.Tests.IntegrationTests.TestData
{
    public static class MonkifyIntegrationTestsStubs
    {
        public static SessionParameters GenerateSession() => new SessionParameters()
        {
            Name = "Four Letter Race",
            Description = "Type a Four-letter word and hope that Edson types it before anyone else!",
            AllowedCharacters = Domain.Sessions.ValueObjects.SessionCharacterType.Letters,
            RequiredAmount = 1,
            MinimumNumberOfPlayers = 2,
            ChoiceRequiredLength = 4,
            AcceptDuplicatedCharacters = true,
            Active = true,
        };

        public static RegisterBetRequestBody GenerateBet() => new RegisterBetRequestBody()
        {
            PaymentSignature = "4gQMQT3avWvzxFm9W5Q5eY8vMhR1yaEBvaSy3pHsmFSvnbKX7iEC3bXgA5bfHs2NGBpPSUtTnxVu1LhwtGfkgXPD",
            Wallet = "22nvbHMDmUYzRqwm3eSo54BeNfPLsvViNNHZwjUpnQ1S",
            Choice = new Faker().Random.String2(4),
            Amount = 1,
            Seed = Guid.NewGuid().ToString()
        };
    }
}
