using System.Collections.Concurrent;
using System.Diagnostics;
using Bogus;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using Shouldly;
using Xunit.Abstractions;

namespace Monkify.Tests.Shared
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

        protected const int NUMBER_OF_PARALLEL_SESSIONS = 8;
        protected const int MAX_DURATION_FOR_SESSION = 10;

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
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, // Define o grau de paralelismo
                async (_, cancellationToken) =>
                {
                    result.Add(await RunSession(parameters));
                });

            return result;
        }

        protected Task<SessionMetricsResult> RunSession(SessionMetricsTestParameters parameters)
        {
            var sessionParameters = new SessionParameters(parameters.CharacterType, parameters.WordLength, parameters.AcceptsDuplicateCharacters);
            var bets = CreateBetList(parameters.BetsPerGame, parameters.WordLength, parameters.Charset);
             
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

            return Task.FromResult(new SessionMetricsResult(watch.Elapsed, numberOfBatches));
        }

        private ConcurrentBag<Bet> CreateBetList(int numberOfBets, int wordLength, string charset)
        {
            var result = new ConcurrentBag<Bet>();
            for (int i = 1; i <= numberOfBets; i++)
            {
                result.Add(new(BetStatus.Made, 10, Faker.Random.String2(wordLength, charset), Faker.Random.String2(40, "abcdefghijklmnopqrstuvwxyz0123456789 ")));
            }
            return result;
        }
    }
}
