using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Bogus;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Monkify.Common.Extensions;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Services.Sessions;
using Moq;
using Shouldly;
using Xunit.Abstractions;

namespace Monkify.Tests.UnitTests.Shared
{
    public abstract class BaseSessionMetricsTestsClass
    {

        public BaseSessionMetricsTestsClass(ITestOutputHelper console)
        {
            Faker = new Faker();
            Console = console;
        }


        protected const int MAX_DURATION_FOR_SESSION = 10;
        
        protected readonly ITestOutputHelper Console;
        protected readonly Faker Faker;

        protected async Task<ConcurrentDictionary<int, SessionMetricsResult>> RunMultipleSessions(SessionMetricsTestParameters parameters)
        {
            var result = new ConcurrentDictionary<int, SessionMetricsResult>();

            var parallelismDegree = Environment.ProcessorCount / 2;

            await Parallel.ForEachAsync(
                Enumerable.Range(0, parallelismDegree),
                new ParallelOptions { MaxDegreeOfParallelism = parallelismDegree },
                async (index, cancellationToken) =>
                {
                    var sessionResult = await RunSession(parameters);
                    result.TryAdd(index + 1, sessionResult);
                }
            );

            return result;
        }

        protected async Task<SessionMetricsResult> RunSession(SessionMetricsTestParameters parameters)
        {
            var factory = new SessionServiceTestFactory();
            var service = factory.Create();
            var sessionParameters = new SessionParameters(parameters.CharacterType, parameters.WordLength, true);

            var session = new Session(sessionParameters, [.. factory.CreateBets(parameters.CharacterType, parameters.WordLength, parameters.BetsPerGame)]);
            var monkey = new MonkifyTyper(session);

            var watch = Stopwatch.StartNew();
            await service.RunSession(session, CancellationToken.None);
            watch.Stop();

            return new SessionMetricsResult(watch.Elapsed, factory.NumberOfBatches);
        }

        protected void ValidateSessionRuns(IDictionary<int, SessionMetricsResult> sessionResults)
        {
            Console.WriteLine("Session results:");
            foreach (var result in sessionResults)
            {
                Console.WriteLine($"Session {result.Key}: {result.Value}");
            }

            sessionResults.Any(x => x.Value.Duration.TotalSeconds > MAX_DURATION_FOR_SESSION).ShouldBeFalse();
        }
    }
}
