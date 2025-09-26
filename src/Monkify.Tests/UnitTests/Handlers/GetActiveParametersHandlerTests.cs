using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Handlers.Sessions.GetActiveParameters;
using Monkify.Tests.UnitTests.Shared;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Monkify.Tests.UnitTests.Handlers
{
    public class GetActiveParametersHandlerTests : UnitTestsClass
    {
        [Fact]
        public async Task HandleRequest_NoParameters_ShouldReturnEmptyList()
        {
            using (var context = new MonkifyDbContext(ContextOptions))
            {
                var request = new GetActiveParametersRequest();
                var handler = new GetActiveParametersHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.ShouldNotBeNull();
                result.Count().ShouldBe(0);

                Messaging.HasErrors().ShouldBeFalse();
                Messaging.HasValidationFailures().ShouldBeFalse();
            }
        }

        [Fact]
        public async Task HandleRequest_OnlyInactiveParameters_ShouldReturnEmptyList()
        {
            var inactiveParameter = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                Description = Faker.Random.Words(6),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2,
                AllowedCharacters = SessionCharacterType.Letters,
                Active = false
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.SessionParameters.Add(inactiveParameter);
                context.SaveChanges();

                var request = new GetActiveParametersRequest();
                var handler = new GetActiveParametersHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.ShouldNotBeNull();
                result.Count().ShouldBe(0);

                Messaging.HasErrors().ShouldBeFalse();
                Messaging.HasValidationFailures().ShouldBeFalse();
            }
        }

        [Fact]
        public async Task HandleRequest_SingleActiveParameter_ShouldReturnSingleDto()
        {
            var activeParameter = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                Description = Faker.Random.Words(6),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2.5m,
                MinimumNumberOfPlayers = 3,
                AllowedCharacters = SessionCharacterType.Letters,
                Active = true
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.SessionParameters.Add(activeParameter);
                context.SaveChanges();

                var request = new GetActiveParametersRequest();
                var handler = new GetActiveParametersHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.ShouldNotBeNull();
                result.Count().ShouldBe(1);

                var dto = result.First();
                dto.SessionTypeId.ShouldBe(activeParameter.Id);
                dto.SessionCharacterType.ShouldBe(activeParameter.AllowedCharacters);
                dto.Name.ShouldBe(activeParameter.Name);
                dto.Description.ShouldBe(activeParameter.Description);
                dto.RequiredAmount.ShouldBe(activeParameter.RequiredAmount);
                dto.MinimumNumberOfPlayers.ShouldBe(activeParameter.MinimumNumberOfPlayers);
                dto.ChoiceRequiredLength.ShouldBe(activeParameter.ChoiceRequiredLength);
                dto.AcceptDuplicatedCharacters.ShouldBe(activeParameter.AcceptDuplicatedCharacters);

                Messaging.HasErrors().ShouldBeFalse();
                Messaging.HasValidationFailures().ShouldBeFalse();
            }
        }

        [Fact]
        public async Task HandleRequest_MultipleActiveParameters_ShouldReturnAllDtos()
        {
            var activeParameter1 = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                Description = Faker.Random.Words(6),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2.5m,
                MinimumNumberOfPlayers = 2,
                AllowedCharacters = SessionCharacterType.Letters,
                Active = true
            };

            var activeParameter2 = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                Description = Faker.Random.Words(8),
                AcceptDuplicatedCharacters = false,
                ChoiceRequiredLength = 6,
                RequiredAmount = 5.0m,
                MinimumNumberOfPlayers = 4,
                AllowedCharacters = SessionCharacterType.Number,
                Active = true
            };

            var inactiveParameter = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                Description = Faker.Random.Words(4),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 3,
                RequiredAmount = 1.0m,
                MinimumNumberOfPlayers = 1,
                AllowedCharacters = SessionCharacterType.NumbersAndLetters,
                Active = false
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.SessionParameters.AddRange(activeParameter1, activeParameter2, inactiveParameter);
                context.SaveChanges();

                var request = new GetActiveParametersRequest();
                var handler = new GetActiveParametersHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.ShouldNotBeNull();
                result.Count().ShouldBe(2);

                var dtos = result.ToList();
                dtos.Any(x => x.SessionTypeId == activeParameter1.Id).ShouldBeTrue();
                dtos.Any(x => x.SessionTypeId == activeParameter2.Id).ShouldBeTrue();
                dtos.Any(x => x.SessionTypeId == inactiveParameter.Id).ShouldBeFalse();

                Messaging.HasErrors().ShouldBeFalse();
                Messaging.HasValidationFailures().ShouldBeFalse();
            }
        }

        [Fact]
        public async Task HandleRequest_ActiveParameterWithPresetChoices_ShouldIncludePresetChoicesInDto()
        {
            var activeParameter = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                Description = Faker.Random.Words(6),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2.5m,
                MinimumNumberOfPlayers = 3,
                AllowedCharacters = SessionCharacterType.Letters,
                Active = true,
                PresetChoices = new List<PresetChoice>()
            };

            var presetChoice1 = new PresetChoice("ABCD");
            var presetChoice2 = new PresetChoice("EFGH");
            var presetChoice3 = new PresetChoice("IJKL");

            activeParameter.PresetChoices.Add(presetChoice1);
            activeParameter.PresetChoices.Add(presetChoice2);
            activeParameter.PresetChoices.Add(presetChoice3);

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.SessionParameters.Add(activeParameter);
                context.SaveChanges();

                var request = new GetActiveParametersRequest();
                var handler = new GetActiveParametersHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.ShouldNotBeNull();
                result.Count().ShouldBe(1);

                var dto = result.First();
                dto.PresetChoices.ShouldNotBeNull();
                dto.PresetChoices.Count().ShouldBe(3);
                dto.PresetChoices.ShouldContain("ABCD");
                dto.PresetChoices.ShouldContain("EFGH");
                dto.PresetChoices.ShouldContain("IJKL");

                Messaging.HasErrors().ShouldBeFalse();
                Messaging.HasValidationFailures().ShouldBeFalse();
            }
        }

        [Fact]
        public async Task HandleRequest_ActiveParameterWithoutPresetChoices_ShouldReturnEmptyPresetChoicesList()
        {
            var activeParameter = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                Description = Faker.Random.Words(6),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2.5m,
                MinimumNumberOfPlayers = 3,
                AllowedCharacters = SessionCharacterType.Letters,
                Active = true,
                PresetChoices = new List<PresetChoice>()
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.SessionParameters.Add(activeParameter);
                context.SaveChanges();

                var request = new GetActiveParametersRequest();
                var handler = new GetActiveParametersHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.ShouldNotBeNull();
                result.Count().ShouldBe(1);

                var dto = result.First();
                dto.PresetChoices.ShouldNotBeNull();
                dto.PresetChoices.Count().ShouldBe(0);

                Messaging.HasErrors().ShouldBeFalse();
                Messaging.HasValidationFailures().ShouldBeFalse();
            }
        }

        [Theory]
        [InlineData(SessionCharacterType.Letters)]
        [InlineData(SessionCharacterType.Number)]
        [InlineData(SessionCharacterType.NumbersAndLetters)]
        public async Task HandleRequest_ParameterWithDifferentCharacterTypes_ShouldMapCorrectly(SessionCharacterType characterType)
        {
            var activeParameter = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                Description = Faker.Random.Words(6),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2.5m,
                MinimumNumberOfPlayers = 3,
                AllowedCharacters = characterType,
                Active = true
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.SessionParameters.Add(activeParameter);
                context.SaveChanges();

                var request = new GetActiveParametersRequest();
                var handler = new GetActiveParametersHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.ShouldNotBeNull();
                result.Count().ShouldBe(1);

                var dto = result.First();
                dto.SessionCharacterType.ShouldBe(characterType);

                Messaging.HasErrors().ShouldBeFalse();
                Messaging.HasValidationFailures().ShouldBeFalse();
            }
        }

        [Fact]
        public async Task HandleRequest_ActiveParameterWithNullChoiceRequiredLength_ShouldMapNullCorrectly()
        {
            var activeParameter = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                Description = Faker.Random.Words(6),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = null,
                RequiredAmount = 2.5m,
                MinimumNumberOfPlayers = 3,
                AllowedCharacters = SessionCharacterType.Letters,
                Active = true
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.SessionParameters.Add(activeParameter);
                context.SaveChanges();

                var request = new GetActiveParametersRequest();
                var handler = new GetActiveParametersHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.ShouldNotBeNull();
                result.Count().ShouldBe(1);

                var dto = result.First();
                dto.ChoiceRequiredLength.ShouldBeNull();

                Messaging.HasErrors().ShouldBeFalse();
                Messaging.HasValidationFailures().ShouldBeFalse();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task HandleRequest_ParameterWithDifferentAcceptDuplicatedCharacters_ShouldMapCorrectly(bool acceptDuplicatedCharacters)
        {
            var activeParameter = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                Description = Faker.Random.Words(6),
                AcceptDuplicatedCharacters = acceptDuplicatedCharacters,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2.5m,
                MinimumNumberOfPlayers = 3,
                AllowedCharacters = SessionCharacterType.Letters,
                Active = true
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.SessionParameters.Add(activeParameter);
                context.SaveChanges();

                var request = new GetActiveParametersRequest();
                var handler = new GetActiveParametersHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.ShouldNotBeNull();
                result.Count().ShouldBe(1);

                var dto = result.First();
                dto.AcceptDuplicatedCharacters.ShouldBe(acceptDuplicatedCharacters);

                Messaging.HasErrors().ShouldBeFalse();
                Messaging.HasValidationFailures().ShouldBeFalse();
            }
        }

        [Fact]
        public async Task HandleRequest_ComplexScenarioWithMixedActiveInactiveParametersAndPresetChoices_ShouldReturnCorrectDtos()
        {
            // Active parameter with preset choices
            var activeParameterWithPresets = new SessionParameters()
            {
                Name = "Active With Presets",
                Description = "Active parameter with preset choices",
                AcceptDuplicatedCharacters = false,
                ChoiceRequiredLength = 5,
                RequiredAmount = 3.75m,
                MinimumNumberOfPlayers = 2,
                AllowedCharacters = SessionCharacterType.NumbersAndLetters,
                Active = true,
                PresetChoices = new List<PresetChoice>
                {
                    new PresetChoice("MIXED1"),
                    new PresetChoice("MIXED2")
                }
            };

            // Active parameter without preset choices
            var activeParameterWithoutPresets = new SessionParameters()
            {
                Name = "Active Without Presets",
                Description = "Active parameter without preset choices",
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 3,
                RequiredAmount = 1.25m,
                MinimumNumberOfPlayers = 1,
                AllowedCharacters = SessionCharacterType.Number,
                Active = true,
                PresetChoices = new List<PresetChoice>()
            };

            // Inactive parameter (should not appear in results)
            var inactiveParameter = new SessionParameters()
            {
                Name = "Inactive Parameter",
                Description = "This should not appear",
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 6,
                RequiredAmount = 10.0m,
                MinimumNumberOfPlayers = 5,
                AllowedCharacters = SessionCharacterType.Letters,
                Active = false,
                PresetChoices = new List<PresetChoice>
                {
                    new PresetChoice("SHOULD"),
                    new PresetChoice("NOT"),
                    new PresetChoice("APPEAR")
                }
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.SessionParameters.AddRange(activeParameterWithPresets, activeParameterWithoutPresets, inactiveParameter);
                context.SaveChanges();

                var request = new GetActiveParametersRequest();
                var handler = new GetActiveParametersHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.ShouldNotBeNull();
                result.Count().ShouldBe(2);

                var dtos = result.ToList();

                // Verify active parameter with presets
                var dtoWithPresets = dtos.FirstOrDefault(x => x.SessionTypeId == activeParameterWithPresets.Id);
                dtoWithPresets.ShouldNotBeNull();
                dtoWithPresets.Name.ShouldBe("Active With Presets");
                dtoWithPresets.Description.ShouldBe("Active parameter with preset choices");
                dtoWithPresets.SessionCharacterType.ShouldBe(SessionCharacterType.NumbersAndLetters);
                dtoWithPresets.AcceptDuplicatedCharacters.ShouldBeFalse();
                dtoWithPresets.ChoiceRequiredLength.ShouldBe(5);
                dtoWithPresets.RequiredAmount.ShouldBe(3.75m);
                dtoWithPresets.MinimumNumberOfPlayers.ShouldBe(2);
                dtoWithPresets.PresetChoices.ShouldNotBeNull();
                dtoWithPresets.PresetChoices.Count().ShouldBe(2);
                dtoWithPresets.PresetChoices.ShouldContain("MIXED1");
                dtoWithPresets.PresetChoices.ShouldContain("MIXED2");

                // Verify active parameter without presets
                var dtoWithoutPresets = dtos.FirstOrDefault(x => x.SessionTypeId == activeParameterWithoutPresets.Id);
                dtoWithoutPresets.ShouldNotBeNull();
                dtoWithoutPresets.Name.ShouldBe("Active Without Presets");
                dtoWithoutPresets.Description.ShouldBe("Active parameter without preset choices");
                dtoWithoutPresets.SessionCharacterType.ShouldBe(SessionCharacterType.Number);
                dtoWithoutPresets.AcceptDuplicatedCharacters.ShouldBeTrue();
                dtoWithoutPresets.ChoiceRequiredLength.ShouldBe(3);
                dtoWithoutPresets.RequiredAmount.ShouldBe(1.25m);
                dtoWithoutPresets.MinimumNumberOfPlayers.ShouldBe(1);
                dtoWithoutPresets.PresetChoices.ShouldNotBeNull();
                dtoWithoutPresets.PresetChoices.Count().ShouldBe(0);

                // Verify inactive parameter is not included
                dtos.Any(x => x.SessionTypeId == inactiveParameter.Id).ShouldBeFalse();

                Messaging.HasErrors().ShouldBeFalse();
                Messaging.HasValidationFailures().ShouldBeFalse();
            }
        }
    }
}