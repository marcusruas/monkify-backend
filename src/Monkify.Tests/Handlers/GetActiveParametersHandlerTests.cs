using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Handlers.Sessions.GetActiveParameters;
using Monkify.Tests.Shared;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.Handlers
{
    public class GetActiveParametersHandlerTests : UnitTestsClass
    {
        [Fact]
        public async Task GetParameters_ShouldReturnData()
        {
            var parameters = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2,
                SessionCharacterType = SessionCharacterType.LowerCaseLetter,
                PresetChoices = new List<PresetChoice>() { new PresetChoice(Faker.Random.String2(4)), new PresetChoice(Faker.Random.String2(4)), new PresetChoice(Faker.Random.String2(4)) },
                Active = true,
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(parameters);
                context.SaveChanges();

                var request = new GetActiveParametersRequest();
                var handler = new GetActiveParametersHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.ShouldNotBeNull();
                result.ShouldNotBeEmpty();

                var firstItem = result.FirstOrDefault();

                firstItem.ShouldNotBeNull();
                firstItem.SessionTypeId.ShouldBe(parameters.Id);
                firstItem.Name.ShouldBe(parameters.Name);
                firstItem.PresetChoices.IsNullOrEmpty().ShouldBeFalse();
            }
        }

        [Fact]
        public async Task GetParameters_NoPresetChoices_ShouldReturnEmptyChoices()
        {
            var parameters = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2,
                SessionCharacterType = SessionCharacterType.LowerCaseLetter,
                Active = true,
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(parameters);
                context.SaveChanges();

                var request = new GetActiveParametersRequest();
                var handler = new GetActiveParametersHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.ShouldNotBeNull();
                result.ShouldNotBeEmpty();

                var firstItem = result.FirstOrDefault();

                firstItem.ShouldNotBeNull();
                firstItem.SessionTypeId.ShouldBe(parameters.Id);
                firstItem.Name.ShouldBe(parameters.Name);
                firstItem.PresetChoices.ShouldNotBeNull();
                firstItem.PresetChoices.ShouldBeEmpty();
            }
        }



        [Fact]
        public async Task GetParameters_NoActiveParameters_ShouldReturnEmptyList()
        {
            var parameters = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2,
                SessionCharacterType = SessionCharacterType.LowerCaseLetter,
                PresetChoices = new List<PresetChoice>() { new PresetChoice(Faker.Random.String2(4)), new PresetChoice(Faker.Random.String2(4)), new PresetChoice(Faker.Random.String2(4)) },
                Active = false,
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(parameters);
                context.SaveChanges();

                var request = new GetActiveParametersRequest();
                var handler = new GetActiveParametersHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.ShouldNotBeNull();
                result.ShouldBeEmpty();
            }
        }
    }
}
