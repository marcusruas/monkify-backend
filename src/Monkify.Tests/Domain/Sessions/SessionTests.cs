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
        }

        [Fact]
        public void UpdateStatus_ToInProgress_ShouldUpdateStatus()
        {
            var session = new Session();
            var newStatus = SessionStatus.Started;

            session.UpdateStatus(newStatus);

            session.Status.ShouldBe(newStatus);
            session.EndDate.ShouldBeNull();
            session.WinningChoice.ShouldBeNull();
        }

        [Fact]
        public void UpdateStatus_ToEnded_ShouldSetEndDateAndWinningChoice()
        {
            var session = new Session();
            var endStatus = SessionStatus.Ended;
            var winningChoice = "Option1";

            session.UpdateStatus(endStatus, winningChoice);

            session.Status.ShouldBe(endStatus);
            session.EndDate.ShouldNotBeNull();
            session.WinningChoice.ShouldBe(winningChoice);
        }
    }
}
