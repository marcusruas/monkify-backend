using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using Shouldly;

namespace Monkify.Tests.Domain.Sessions
{
    public class SessionTests
    {
        [Fact]
        public void Constructor_InitializesLogs()
        {
            var session = new Session();

            session.Logs.ShouldNotBeNull();
            session.Logs.ShouldBeEmpty();
        }

        [Fact]
        public void ConstructorWithParameter_InitializesProperties()
        {
            var parametersId = Guid.NewGuid();
            var session = new Session(parametersId);

            session.ParametersId.ShouldBe(parametersId);
            session.Status.ShouldBe(SessionStatus.WaitingBets);
            session.Logs.ShouldNotBeNull();
            session.Logs.Count.ShouldBe(1);
            session.Logs.First().NewStatus.ShouldBe(SessionStatus.WaitingBets);
        }

        [Fact]
        public void UpdateStatus_SetsEndDate_WhenSessionEnded()
        {
            var session = new Session();
            session.UpdateStatus(SessionStatus.Ended);

            session.EndDate.ShouldNotBeNull();
            session.Status.ShouldBe(SessionStatus.Ended);
        }

        [Fact]
        public void UpdateStatus_DoesNotSetEndDate_WhenSessionInProgress()
        {
            var session = new Session();
            session.UpdateStatus(SessionStatus.WaitingBets);

            session.EndDate.ShouldBeNull();
            session.Status.ShouldBe(SessionStatus.WaitingBets);
        }
    }
}
