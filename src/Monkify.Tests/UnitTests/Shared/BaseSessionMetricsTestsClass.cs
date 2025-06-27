using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Bogus;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using Shouldly;
using Xunit.Abstractions;

namespace Monkify.Tests.UnitTests.Shared
{
    public abstract class BaseSessionMetricsTestsClass
    {
        protected readonly ITestOutputHelper Console;
        protected readonly Faker Faker;

        public BaseSessionMetricsTestsClass(ITestOutputHelper console)
        {
            Faker = new Faker();
            Console = console;
        }

        protected const int NUMBER_OF_PARALLEL_SESSIONS = 10;

        protected const int MAX_DURATION_FOR_SESSION = 5;

        protected void ValidateSessionRuns(IEnumerable<SessionMetricsResult> sessionResults)
        {
            Console.WriteLine("Session results:");
            foreach (var result in sessionResults)
            {
                Console.WriteLine(result.ToString());
            }

            sessionResults.Any(x => x.Duration.TotalSeconds > MAX_DURATION_FOR_SESSION).ShouldBeFalse();
        }

        protected async Task<ConcurrentBag<SessionMetricsResult>> RunMultipleSessions(SessionMetricsTestParameters parameters)
        {
            var result = new ConcurrentBag<SessionMetricsResult>();

            await Parallel.ForEachAsync(
                Enumerable.Range(0, NUMBER_OF_PARALLEL_SESSIONS),
                new ParallelOptions { MaxDegreeOfParallelism = 4 },
                async (_, cancellationToken) =>
                {
                    result.Add(await RunSession(parameters));
                }
            );

            return result;
        }

        protected async Task<SessionMetricsResult> RunSession(SessionMetricsTestParameters parameters)
        {
            await Task.Yield();

            var sessionParameters = new SessionParameters(parameters.CharacterType, parameters.WordLength, parameters.AcceptsDuplicateCharacters);
            sessionParameters.PresetChoices = parameters.PresetChoices.Select(x => new PresetChoice(x)).ToList();
            var bets = CreateBetList(parameters.PresetChoices, parameters.BetsPerGame, parameters.WordLength, parameters.Charset, parameters.AcceptsDuplicateCharacters);

            int terminalBatchLimit = 100;
            int numberOfBatches = 0;
            var session = new Session(sessionParameters, [.. bets]);
            var monkey = new MonkifyTyper(session);

            var watch = Stopwatch.StartNew();
            char[] batch = new char[terminalBatchLimit];
            int batchIndex = 0;

            while (!monkey.HasWinners)
            {
                batch[batchIndex++] = monkey.GenerateNextCharacter();

                if (batchIndex >= terminalBatchLimit - 1)
                {
                    batchIndex = 0;
                    numberOfBatches++;
                    continue;
                }
            }
            watch.Stop();

            return new SessionMetricsResult(watch.Elapsed, numberOfBatches);
        }

        private ConcurrentBag<Bet> CreateBetList(List<string> presetChoices, int numberOfBets, int wordLength, string charset, bool allowDuplicateCharacters)
        {
            var result = new ConcurrentBag<Bet>();

            if (!presetChoices.IsNullOrEmpty())
            {
                string seed = Faker.Random.String2(40, "abcdefghijklmnopqrstuvwxyz0123456789 ");

                foreach (var choice in presetChoices)
                {
                    result.Add(new(BetStatus.Made, 10, choice, seed));
                }
            }
            else
            {
                for (int i = 1; i <= numberOfBets; i++)
                {
                    string choice = GenerateRandomString(wordLength, charset, allowDuplicateCharacters);
                    string seed = Faker.Random.String2(40, "abcdefghijklmnopqrstuvwxyz0123456789 ");
                    result.Add(new(BetStatus.Made, 10, choice, seed));
                }
            }

            return result;
        }

        static string GenerateRandomString(int length, string charset, bool allowDuplicates)
        {
            if (string.IsNullOrEmpty(charset))
                throw new ArgumentException("Charset cannot be null or empty.");

            if (!allowDuplicates && length > charset.Length)
                throw new ArgumentException("Length cannot be greater than the charset size when duplicates are not allowed.");

            Random random = new Random();
            char[] result = new char[length];

            if (allowDuplicates)
            {
                for (int i = 0; i < length; i++)
                {
                    result[i] = charset[random.Next(charset.Length)];
                }
            }
            else
            {
                var shuffledCharset = charset.OrderBy(c => random.Next()).ToList();
                for (int i = 0; i < length; i++)
                {
                    result[i] = shuffledCharset[i];
                }
            }

            return new string(result);
        }
    }
}
