using Bogus;
using Microsoft.EntityFrameworkCore;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Handlers.Sessions.GetAllBets;
using Monkify.Tests.Shared;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.Handlers
{
    public class FilterBetsHandlerTests : UnitTestsClass
    {
        [Fact]
        public async Task FilterBets_CorrectFilter_ShouldReturnSuccess()
        {
            var session = new Session();
            session.Status = SessionStatus.WaitingBets;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, AllowedCharacters = SessionCharacterType.Letters };
            session.Bets = new List<Bet>() 
            { 
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var request = new FilterBetsRequest(1, 10);
                var handler = new FilterBetsHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.CurrentPage.ShouldBe(1);
                result.Items.ShouldNotBeNull();
                result.Items.ShouldNotBeEmpty();
                result.Items.Count().ShouldBe(5);
                result.Items.All(x => !x.Won).ShouldBeTrue();
                result.TotalNumberOfPages.ShouldBe(1);
                result.TotalNumberOfRecords.ShouldBe(5);
            }
        }

        [Fact]
        public async Task FilterBets_NoPublicBets_ShouldReturnSuccess()
        {
            var presetChoices = new List<PresetChoice>() { new PresetChoice(Faker.Random.String2(4)), new PresetChoice(Faker.Random.String2(4)) };
            var session = new Session();
            session.Status = SessionStatus.WaitingBets;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, AllowedCharacters = SessionCharacterType.Letters, PresetChoices = presetChoices };
            session.Bets = new List<Bet>()
            {
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.Refunded },
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.NeedsManualAnalysis },
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var request = new FilterBetsRequest(1, 10);
                var handler = new FilterBetsHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.CurrentPage.ShouldBe(1);
                result.Items.ShouldNotBeNull();
                result.Items.ShouldBeEmpty();
                result.Items.Count().ShouldBe(0);
                result.TotalNumberOfPages.ShouldBe(1);
                result.TotalNumberOfRecords.ShouldBe(0);
            }
        }

        [Fact]
        public async Task FilterBets_MultiplePages_ShouldReturnSuccess()
        {
            var presetChoices = new List<PresetChoice>() { new PresetChoice(Faker.Random.String2(4)), new PresetChoice(Faker.Random.String2(4)) };
            var session = new Session();
            session.Status = SessionStatus.WaitingBets;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, AllowedCharacters = SessionCharacterType.Letters, PresetChoices = presetChoices };
            session.Bets = new List<Bet>()
            {
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var request = new FilterBetsRequest(1, 2);
                var handler = new FilterBetsHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.CurrentPage.ShouldBe(1);
                result.Items.ShouldNotBeNull();
                result.Items.ShouldNotBeEmpty();
                result.Items.Count().ShouldBe(2);
                result.TotalNumberOfPages.ShouldBe(3);
                result.TotalNumberOfRecords.ShouldBe(5);
            }
        }

        [Fact]
        public async Task FilterBets_NoBets_ShouldReturnSuccess()
        {
            using (var context = new MonkifyDbContext(ContextOptions))
            {
                var request = new FilterBetsRequest(1, 10);
                var handler = new FilterBetsHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.CurrentPage.ShouldBe(1);
                result.Items.ShouldNotBeNull();
                result.Items.ShouldBeEmpty();
                result.Items.Count().ShouldBe(0);
                result.TotalNumberOfPages.ShouldBe(1);
                result.TotalNumberOfRecords.ShouldBe(0);
            }
        }
    }
}
