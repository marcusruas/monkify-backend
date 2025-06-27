using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using Shouldly;

namespace Monkify.Tests.UnitTests.Domain.Sessions
{
    public class SessionTests
    {
        [Fact]
        public void DefaultConstructor_ShouldInitializeWithDefaultValues()
        {
            var session = new Session();

            session.Status.ShouldBe(SessionStatus.WaitingBets);
            session.StatusLogs.ShouldNotBeEmpty();
            session.StatusLogs.First().NewStatus.ShouldBe(SessionStatus.WaitingBets);
            session.Bets.ShouldBeEmpty();
        }

        [Fact]
        public void ConstructorWithParametersId_ShouldInitializeWithParametersId()
        {
            var parametersId = Guid.NewGuid();

            var session = new Session(parametersId);

            session.ParametersId.ShouldBe(parametersId);
            session.Status.ShouldBe(SessionStatus.WaitingBets);
        }
    }
}
