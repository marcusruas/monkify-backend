using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Services.Solana;
using Monkify.Tests.Shared;
using Moq;
using Shouldly;
using Solnet.Rpc;
using Solnet.Rpc.Core.Http;
using Solnet.Rpc.Messages;
using Solnet.Rpc.Models;
using Solnet.Rpc.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.Services
{
    public class SolanaServiceTests : UnitTestsClass
    {
        public SolanaServiceTests()
        {
            _rpcClientMock = new();
            _settings = new()
            {
                Polly = new()
                { 
                    GetTransactionRetryCount = 1,
                    LatestBlockshashRetryCount = 3
                },
                Token = new()
                {
                    SenderAccount = "HJ5z5cmD76tAN1kxSWuvuAxwmtpaXxGvHxEbF2FBAinY",
                    TokenOwnerPublicKey = "GHxPmQcC4s6iGi9bAFtsw75wB2RprqXGXbEewTcN7EyN",
                    TokenOwnerPrivateKey = "2t4t5zEMppzldKqDyk7N8N341eRDbE0qs8O2evkNXlrjNzq21fXcIHmnBviqn4YeNp8kbO9q/Iy6kT/9Kb8UOQ=="
                }
            };
        }

        private readonly GeneralSettings _settings;
        private readonly Mock<IRpcClient> _rpcClientMock;
        private const string LATEST_BLOCKHASH = "EkSnNWid2cvwEVnVx9aBqawnmiCNiDgp3gUdkDPTKN1N";
        private const string WALLET_FOR_TESTS = "5sQvEQfJGZgfoRoGdqaHS1bEudiAYToaUjxdb4dHLaym";

        [Fact]
        public async Task SetLatestBlockhashForTokenTransfer_ShouldRunSuccessfully()
        {
            var result = new RequestResult<ResponseValue<LatestBlockHash>>()
            {
                WasHttpRequestSuccessful = true,
                Result = new()
                {
                    Value = new()
                    {
                        Blockhash = LATEST_BLOCKHASH
                    }
                }
            };
            _rpcClientMock.Setup(x => x.GetLatestBlockHashAsync(It.IsAny<Commitment>())).Returns(Task.FromResult(result));

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                var service = new SolanaService(context, _rpcClientMock.Object, _settings);

                var blockhashObtained = await service.SetLatestBlockhashForTokenTransfer();

                blockhashObtained.ShouldBeTrue();
                _rpcClientMock.Verify(x => x.GetLatestBlockHashAsync(It.IsAny<Commitment>()), Times.Exactly(1));
            }
        }

        [Fact]
        public async Task SetLatestBlockhashForTokenTransfer_HttpRequestFailed_ShouldReturnFalse()
        {
            var result = new RequestResult<ResponseValue<LatestBlockHash>>()
            {
                WasHttpRequestSuccessful = false,
                Result = new()
                {
                    Value = new()
                    {
                        Blockhash = LATEST_BLOCKHASH
                    }
                }
            };
            _rpcClientMock.Setup(x => x.GetLatestBlockHashAsync(It.IsAny<Commitment>())).Returns(Task.FromResult(result));

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                var service = new SolanaService(context, _rpcClientMock.Object, _settings);

                var blockhashObtained = await service.SetLatestBlockhashForTokenTransfer();

                blockhashObtained.ShouldBeFalse();
                _rpcClientMock.Verify(x => x.GetLatestBlockHashAsync(It.IsAny<Commitment>()), Times.Exactly(4));
            }
        }

        [Fact]
        public async Task SetLatestBlockhashForTokenTransfer_NullBlockhash_ShouldReturnFalse()
        {
            var result = new RequestResult<ResponseValue<LatestBlockHash>>()
            {
                WasHttpRequestSuccessful = true,
                Result = new()
                {
                    Value = new()
                    {
                        Blockhash = null
                    }
                }
            };
            _rpcClientMock.Setup(x => x.GetLatestBlockHashAsync(It.IsAny<Commitment>())).Returns(Task.FromResult(result));

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                var service = new SolanaService(context, _rpcClientMock.Object, _settings);

                var blockhashObtained = await service.SetLatestBlockhashForTokenTransfer();

                blockhashObtained.ShouldBeFalse();
                _rpcClientMock.Verify(x => x.GetLatestBlockHashAsync(It.IsAny<Commitment>()), Times.Exactly(4));
            }
        }

        [Fact]
        public async Task TransferTokensForBet_ValidBet_ShouldReturnTrue()
        {
            var blockhash = new RequestResult<ResponseValue<LatestBlockHash>>()
            {
                WasHttpRequestSuccessful = true,
                Result = new()
                {
                    Value = new()
                    {
                        Blockhash = LATEST_BLOCKHASH
                    }
                }
            };
            var result = new RequestResult<string>()
            {
                WasHttpRequestSuccessful = true,
                WasRequestSuccessfullyHandled = true,
                Result = Faker.Random.String2(88)
            };
            _rpcClientMock.Setup(x => x.GetLatestBlockHashAsync(It.IsAny<Commitment>())).Returns(Task.FromResult(blockhash));
            _rpcClientMock.Setup(x => x.SendTransactionAsync(It.IsAny<byte[]>(), It.IsAny<bool>(), It.IsAny<Commitment>())).Returns(Task.FromResult(result));

            var session = new Session();
            session.Status = SessionStatus.RewardForWinnersInProgress;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter };
            var bet = new Bet(session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(40), Faker.Random.String2(4), 2) { Wallet = WALLET_FOR_TESTS };
            session.Bets.Add(bet);
            var transaction = new BetTransactionAmountResult(2, (ulong) (2 * Math.Pow(10, 5)));
            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.Add(bet);
                context.SaveChanges();

                var service = new SolanaService(context, _rpcClientMock.Object, _settings);

                await service.SetLatestBlockhashForTokenTransfer();
                var transactionSuccessful = await service.TransferTokensForBet(bet, transaction);

                transactionSuccessful.ShouldBeTrue();
                context.TransactionLogs.Any(x => x.BetId == bet.Id).ShouldBeTrue();
                _rpcClientMock.Verify(x => x.SendTransactionAsync(It.IsAny<byte[]>(), It.IsAny<bool>(), It.IsAny<Commitment>()), Times.Exactly(1));
            }
        }

        [Fact]
        public async Task TransferTokensForBet_ZeroTokens_ShouldReturnTrue()
        {
            var blockhash = new RequestResult<ResponseValue<LatestBlockHash>>()
            {
                WasHttpRequestSuccessful = true,
                Result = new()
                {
                    Value = new()
                    {
                        Blockhash = LATEST_BLOCKHASH
                    }
                }
            };
            var result = new RequestResult<string>()
            {
                WasHttpRequestSuccessful = true,
                WasRequestSuccessfullyHandled = true,
                Result = Faker.Random.String2(88)
            };
            _rpcClientMock.Setup(x => x.GetLatestBlockHashAsync(It.IsAny<Commitment>())).Returns(Task.FromResult(blockhash));
            _rpcClientMock.Setup(x => x.SendTransactionAsync(It.IsAny<byte[]>(), It.IsAny<bool>(), It.IsAny<Commitment>())).Returns(Task.FromResult(result));

            var bet = new Bet(BetStatus.NeedsRewarding, 2) { Wallet = WALLET_FOR_TESTS };
            var transaction = new BetTransactionAmountResult(0, 0);
            using (var context = new MonkifyDbContext(ContextOptions))
            {
                var service = new SolanaService(context, _rpcClientMock.Object, _settings);

                await service.SetLatestBlockhashForTokenTransfer();
                var transactionSuccessful = await service.TransferTokensForBet(bet, transaction);

                transactionSuccessful.ShouldBeTrue();
                context.TransactionLogs.Any(x => x.BetId == bet.Id).ShouldBeFalse();
                _rpcClientMock.Verify(x => x.SendTransactionAsync(It.IsAny<byte[]>(), It.IsAny<bool>(), It.IsAny<Commitment>()), Times.Never());
            }
        }

        [Fact]
        public async Task TransferTokensForBet_FailedToSendTransaction_ShouldReturnFalse()
        {
            var blockhash = new RequestResult<ResponseValue<LatestBlockHash>>()
            {
                WasHttpRequestSuccessful = true,
                Result = new()
                {
                    Value = new()
                    {
                        Blockhash = LATEST_BLOCKHASH
                    }
                }
            };
            var result = new RequestResult<string>()
            {
                WasHttpRequestSuccessful = false,
                WasRequestSuccessfullyHandled = true,
                Result = Faker.Random.String2(88)
            };
            _rpcClientMock.Setup(x => x.GetLatestBlockHashAsync(It.IsAny<Commitment>())).Returns(Task.FromResult(blockhash));
            _rpcClientMock.Setup(x => x.SendTransactionAsync(It.IsAny<byte[]>(), It.IsAny<bool>(), It.IsAny<Commitment>())).Returns(Task.FromResult(result));

            var bet = new Bet(BetStatus.NeedsRewarding, 2) { Wallet = WALLET_FOR_TESTS };
            var transaction = new BetTransactionAmountResult(2, (ulong)(2 * Math.Pow(10, 5)));
            using (var context = new MonkifyDbContext(ContextOptions))
            {
                var service = new SolanaService(context, _rpcClientMock.Object, _settings);

                await service.SetLatestBlockhashForTokenTransfer();
                var transactionSuccessful = await service.TransferTokensForBet(bet, transaction);

                transactionSuccessful.ShouldBeFalse();
                context.TransactionLogs.Any(x => x.BetId == bet.Id).ShouldBeFalse();
                _rpcClientMock.Verify(x => x.SendTransactionAsync(It.IsAny<byte[]>(), It.IsAny<bool>(), It.IsAny<Commitment>()), Times.Exactly(1));
            }
        }

        [Fact]
        public async Task TransferTokensForBet_ExceptionThrown_ShouldReturnFalse()
        {
            var blockhash = new RequestResult<ResponseValue<LatestBlockHash>>()
            {
                WasHttpRequestSuccessful = true,
                Result = new()
                {
                    Value = new()
                    {
                        Blockhash = LATEST_BLOCKHASH
                    }
                }
            };
            _rpcClientMock.Setup(x => x.GetLatestBlockHashAsync(It.IsAny<Commitment>())).Returns(Task.FromResult(blockhash));
            _rpcClientMock.Setup(x => x.SendTransactionAsync(It.IsAny<byte[]>(), It.IsAny<bool>(), It.IsAny<Commitment>())).Returns(Task.FromResult((RequestResult<string>)null));

            var bet = new Bet(BetStatus.NeedsRewarding, 2) { Wallet = WALLET_FOR_TESTS };
            var transaction = new BetTransactionAmountResult(2, (ulong)(2 * Math.Pow(10, 5)));
            using (var context = new MonkifyDbContext(ContextOptions))
            {
                var service = new SolanaService(context, _rpcClientMock.Object, _settings);

                await service.SetLatestBlockhashForTokenTransfer();
                var transactionSuccessful = await service.TransferTokensForBet(bet, transaction);

                transactionSuccessful.ShouldBeFalse();
                context.TransactionLogs.Any(x => x.BetId == bet.Id).ShouldBeFalse();
                _rpcClientMock.Verify(x => x.SendTransactionAsync(It.IsAny<byte[]>(), It.IsAny<bool>(), It.IsAny<Commitment>()), Times.Exactly(1));
            }
        }
    }
}
