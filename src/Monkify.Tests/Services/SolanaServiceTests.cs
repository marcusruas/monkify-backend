using Monkify.Common.Resources;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Services.Solana;
using Monkify.Tests.Shared;
using Moq;
using Newtonsoft.Json;
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
using Xunit.Sdk;

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
                    GetTransactionRetryCount = 3,
                    LatestBlockshashRetryCount = 3
                },
                Token = new()
                {
                    SenderAccount = "FkdZH693o3s77CVr72hgyaP4LURXqLVnYF69kMB3sFX2",
                    TokenOwnerPublicKey = "GHxPmQcC4s6iGi9bAFtsw75wB2RprqXGXbEewTcN7EyN",
                    TokenOwnerPrivateKey = "5NoRmXjg8ep1WoaPPo1aKfRWerCh6uL9ja4H1MCm5Lj3UjFkc3Y8wukXUsRWG6YR2XbdjWaoTHTRr9weVUontKsN",
                    MintAddress = "FYU5Uxh5mZn8VPLvUq24khhSWK1BN9gXUMG1Gx7RDa1H",
                    Decimals = 9,
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
                WasRequestSuccessfullyHandled = true,
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

                var blockhash = await service.GetLatestBlockhashForTokenTransfer();

                string.IsNullOrWhiteSpace(blockhash).ShouldBeFalse();
                _rpcClientMock.Verify(x => x.GetLatestBlockHashAsync(It.IsAny<Commitment>()), Times.Exactly(1));
            }
        }

        [Fact]
        public async Task SetLatestBlockhashForTokenTransfer_HttpRequestFailed_ShouldReturnFalse()
        {
            var result = new RequestResult<ResponseValue<LatestBlockHash>>()
            {
                WasHttpRequestSuccessful = false,
                WasRequestSuccessfullyHandled = false,
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

                var blockhashObtained = await service.GetLatestBlockhashForTokenTransfer();

                string.IsNullOrWhiteSpace(blockhashObtained).ShouldBeTrue();
                _rpcClientMock.Verify(x => x.GetLatestBlockHashAsync(It.IsAny<Commitment>()), Times.Exactly(4));
            }
        }

        [Fact]
        public async Task SetLatestBlockhashForTokenTransfer_NullBlockhash_ShouldReturnFalse()
        {
            var result = new RequestResult<ResponseValue<LatestBlockHash>>()
            {
                WasHttpRequestSuccessful = true,
                WasRequestSuccessfullyHandled = true,
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

                var blockhash = await service.GetLatestBlockhashForTokenTransfer();

                string.IsNullOrWhiteSpace(blockhash).ShouldBeTrue();
                _rpcClientMock.Verify(x => x.GetLatestBlockHashAsync(It.IsAny<Commitment>()), Times.Exactly(4));
            }
        }

        [Fact]
        public async Task TransferTokensForBet_ValidBet_ShouldReturnTrue()
        {
            var blockhash = new RequestResult<ResponseValue<LatestBlockHash>>()
            {
                WasHttpRequestSuccessful = true,
                WasRequestSuccessfullyHandled = true,
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

                await service.GetLatestBlockhashForTokenTransfer();
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

                await service.GetLatestBlockhashForTokenTransfer();
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
                WasRequestSuccessfullyHandled = true,
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
                WasRequestSuccessfullyHandled = false,
                Result = Faker.Random.String2(88)
            };
            _rpcClientMock.Setup(x => x.GetLatestBlockHashAsync(It.IsAny<Commitment>())).Returns(Task.FromResult(blockhash));
            _rpcClientMock.Setup(x => x.SendTransactionAsync(It.IsAny<byte[]>(), It.IsAny<bool>(), It.IsAny<Commitment>())).Returns(Task.FromResult(result));

            var bet = new Bet(BetStatus.NeedsRewarding, 2) { Wallet = WALLET_FOR_TESTS };
            var transaction = new BetTransactionAmountResult(2, (ulong)(2 * Math.Pow(10, 5)));
            using (var context = new MonkifyDbContext(ContextOptions))
            {
                var service = new SolanaService(context, _rpcClientMock.Object, _settings);

                await service.GetLatestBlockhashForTokenTransfer();
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
                WasRequestSuccessfullyHandled = true,
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

                await service.GetLatestBlockhashForTokenTransfer();
                var transactionSuccessful = await service.TransferTokensForBet(bet, transaction);

                transactionSuccessful.ShouldBeFalse();
                context.TransactionLogs.Any(x => x.BetId == bet.Id).ShouldBeFalse();
                _rpcClientMock.Verify(x => x.SendTransactionAsync(It.IsAny<byte[]>(), It.IsAny<bool>(), It.IsAny<Commitment>()), Times.Exactly(1));
            }
        }

        [Fact(Skip = "Solnet Nuget pkg needs to be fixed in order to test the payment validation properly")]
        public async Task ValidateBetPayment_ValidBet_ShouldReturnValid()
        {
            var transaction = new RequestResult<TransactionMetaSlotInfo>()
            {
                WasHttpRequestSuccessful = true,
                WasRequestSuccessfullyHandled = true,
                Result = MockGetTransactionResult()
            };

            _rpcClientMock.Setup(x => x.GetTransactionAsync(It.IsAny<string>(), It.IsAny<Commitment>())).Returns(Task.FromResult(transaction));
            var session = new Session();
            session.Status = SessionStatus.RewardForWinnersInProgress;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter };
            var bet = new Bet(session.Id, Faker.Random.String2(40), Faker.Random.String2(88), WALLET_FOR_TESTS, "abcd", 10);
            session.Bets.Add(bet);

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var service = new SolanaService(context, _rpcClientMock.Object, _settings);

                var validationResult = await service.ValidateBetPayment(bet);

                _rpcClientMock.Verify(x => x.GetTransactionAsync(It.IsAny<string>(), It.IsAny<Commitment>()), Times.Once());
                validationResult.IsValid.ShouldBeTrue();
                validationResult.ErrorMessage.ShouldBeNull();
            }
        }

        [Fact]
        public async Task ValidateBetPayment_ZeroAmountBet_ShouldReturnValid()
        {
            var transaction = new RequestResult<TransactionMetaSlotInfo>()
            {
                WasHttpRequestSuccessful = true,
                WasRequestSuccessfullyHandled = true,
                Result = MockGetTransactionResult()
            };

            _rpcClientMock.Setup(x => x.GetTransactionAsync(It.IsAny<string>(), It.IsAny<Commitment>())).Returns(Task.FromResult(transaction));
            var session = new Session();
            session.Status = SessionStatus.RewardForWinnersInProgress;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter };
            var bet = new Bet(session.Id, Faker.Random.String2(40), Faker.Random.String2(88), WALLET_FOR_TESTS, "abcd", 0);
            session.Bets.Add(bet);

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var service = new SolanaService(context, _rpcClientMock.Object, _settings);

                var validationResult = await service.ValidateBetPayment(bet);

                _rpcClientMock.Verify(x => x.GetTransactionAsync(It.IsAny<string>(), It.IsAny<Commitment>()), Times.Never());
                validationResult.IsValid.ShouldBeTrue();
                validationResult.ErrorMessage.ShouldBeNull();
            }
        }

        [Fact]
        public async Task ValidateBetPayment_FailedToGetTransaction_ShouldReturnError()
        {
            var transaction = new RequestResult<TransactionMetaSlotInfo>(GetHttpResponseMessage())
            {
                WasHttpRequestSuccessful = true,
                Result = null
            };

            _rpcClientMock.Setup(x => x.GetTransactionAsync(It.IsAny<string>(), It.IsAny<Commitment>())).Returns(Task.FromResult(transaction));
            var session = new Session();
            session.Status = SessionStatus.RewardForWinnersInProgress;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter };
            var bet = new Bet(session.Id, Faker.Random.String2(40), Faker.Random.String2(88), WALLET_FOR_TESTS, "abcd", 10);
            session.Bets.Add(bet);

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var service = new SolanaService(context, _rpcClientMock.Object, _settings);

                var validationResult = await service.ValidateBetPayment(bet);

                _rpcClientMock.Verify(x => x.GetTransactionAsync(It.IsAny<string>(), It.IsAny<Commitment>()), Times.Exactly(4));
                validationResult.IsValid.ShouldBeFalse();
                validationResult.ErrorMessage.ShouldBe(ErrorMessages.InvalidPaymentSignature);
            }
        }

        [Fact(Skip = "Solnet Nuget pkg needs to be fixed in order to test the payment validation properly")]
        public async Task ValidateBetPayment_InvalidSenderOnPayment_ShouldReturnInValid()
        {
            var transaction = new RequestResult<TransactionMetaSlotInfo>()
            {
                WasHttpRequestSuccessful = true,
                WasRequestSuccessfullyHandled = true,
                Result = MockGetTransactionResult()
            };

            _rpcClientMock.Setup(x => x.GetTransactionAsync(It.IsAny<string>(), It.IsAny<Commitment>())).Returns(Task.FromResult(transaction));
            var session = new Session();
            session.Status = SessionStatus.RewardForWinnersInProgress;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter };
            var bet = new Bet(session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(40), "abcd", 10);
            session.Bets.Add(bet);

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var service = new SolanaService(context, _rpcClientMock.Object, _settings);

                var validationResult = await service.ValidateBetPayment(bet);

                _rpcClientMock.Verify(x => x.GetTransactionAsync(It.IsAny<string>(), It.IsAny<Commitment>()), Times.Once());
                validationResult.IsValid.ShouldBeFalse();
                validationResult.ErrorMessage.ShouldBe(ErrorMessages.SignatureWithoutBetAccount);
            }
        }

        [Fact(Skip = "Solnet Nuget pkg needs to be fixed in order to test the payment validation properly")]
        public async Task ValidateBetPayment_InvalidRecipientOnPayment_ShouldReturnInValid()
        {
            _settings.Token.SenderAccount = "3yZe7d9L4jXVBcX3ZqiJR5WgXp9SdhE9uWU9sVfjMDKf";
            var transaction = new RequestResult<TransactionMetaSlotInfo>()
            {
                WasHttpRequestSuccessful = true,
                WasRequestSuccessfullyHandled = true,
                Result = MockGetTransactionResult()
            };

            _rpcClientMock.Setup(x => x.GetTransactionAsync(It.IsAny<string>(), It.IsAny<Commitment>())).Returns(Task.FromResult(transaction));
            var session = new Session();
            session.Status = SessionStatus.RewardForWinnersInProgress;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter };
            var bet = new Bet(session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(40), "abcd", 10);
            session.Bets.Add(bet);

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var service = new SolanaService(context, _rpcClientMock.Object, _settings);

                var validationResult = await service.ValidateBetPayment(bet);

                _rpcClientMock.Verify(x => x.GetTransactionAsync(It.IsAny<string>(), It.IsAny<Commitment>()), Times.Once());
                validationResult.IsValid.ShouldBeFalse();
                validationResult.ErrorMessage.ShouldBe(ErrorMessages.SignatureWithoutOwnerAccount);
            }
        }

        [Fact(Skip = "Solnet Nuget pkg needs to be fixed in order to test the payment validation properly")]
        public async Task ValidateBetPayment_InvalidMintAddress_ShouldReturnInValid()
        {
            _settings.Token.MintAddress = "FYU5Uxh5mZn8VPLvUq24khhSWK1BN9gXUMG1Gx7RDa2H";

            var result = MockGetTransactionResult();

            var transaction = new RequestResult<TransactionMetaSlotInfo>(GetHttpResponseMessage(), result)
            {
                WasHttpRequestSuccessful = true,
                WasRequestSuccessfullyHandled = true,
                Result = result
            };

            _rpcClientMock.Setup(x => x.GetTransactionAsync(It.IsAny<string>(), It.IsAny<Commitment>())).Returns(Task.FromResult(transaction));
            var session = new Session();
            session.Status = SessionStatus.RewardForWinnersInProgress;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter };
            var bet = new Bet(session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(40), "abcd", 10);
            session.Bets.Add(bet);

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var service = new SolanaService(context, _rpcClientMock.Object, _settings);

                var validationResult = await service.ValidateBetPayment(bet);

                _rpcClientMock.Verify(x => x.GetTransactionAsync(It.IsAny<string>(), It.IsAny<Commitment>()), Times.Once());
                validationResult.IsValid.ShouldBeFalse();
                validationResult.ErrorMessage.ShouldBe(ErrorMessages.SignatureForInvalidToken);
            }
        }

        [Fact(Skip = "Solnet Nuget pkg needs to be fixed in order to test the payment validation properly")]
        public async Task ValidateBetPayment_PaymentBiggerThanBet_ShouldReturnInValid()
        {
            var transaction = new RequestResult<TransactionMetaSlotInfo>()
            {
                WasHttpRequestSuccessful = true,
                WasRequestSuccessfullyHandled = true,
                Result = MockGetTransactionResult()
            };

            _rpcClientMock.Setup(x => x.GetTransactionAsync(It.IsAny<string>(), It.IsAny<Commitment>())).Returns(Task.FromResult(transaction));
            var session = new Session();
            session.Status = SessionStatus.RewardForWinnersInProgress;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter };
            var bet = new Bet(session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(40), "abcd", 9);
            session.Bets.Add(bet);

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var service = new SolanaService(context, _rpcClientMock.Object, _settings);

                var validationResult = await service.ValidateBetPayment(bet);

                _rpcClientMock.Verify(x => x.GetTransactionAsync(It.IsAny<string>(), It.IsAny<Commitment>()), Times.Once());
                validationResult.IsValid.ShouldBeFalse();
                validationResult.ErrorMessage.ShouldStartWith(ErrorMessages.SignaturePaidMoreThanBetAmount);
            }
        }

        [Fact(Skip = "Solnet Nuget pkg needs to be fixed in order to test the payment validation properly")]
        public async Task ValidateBetPayment_PaymentLesserThanBet_ShouldReturnInValid()
        {
            var transaction = new RequestResult<TransactionMetaSlotInfo>()
            {
                WasHttpRequestSuccessful = true,
                WasRequestSuccessfullyHandled = true,
                Result = MockGetTransactionResult()
            };

            _rpcClientMock.Setup(x => x.GetTransactionAsync(It.IsAny<string>(), It.IsAny<Commitment>())).Returns(Task.FromResult(transaction));
            var session = new Session();
            session.Status = SessionStatus.RewardForWinnersInProgress;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter };
            var bet = new Bet(session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(40), "abcd", 12);
            session.Bets.Add(bet);

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var service = new SolanaService(context, _rpcClientMock.Object, _settings);

                var validationResult = await service.ValidateBetPayment(bet);

                _rpcClientMock.Verify(x => x.GetTransactionAsync(It.IsAny<string>(), It.IsAny<Commitment>()), Times.Once());
                validationResult.IsValid.ShouldBeFalse();
                validationResult.ErrorMessage.ShouldStartWith(ErrorMessages.SignaturePaidLessThanBetAmount);
            }
        }

        private string TransactionJson
            => "{\"jsonrpc\":\"2.0\",\"result\":{\"blockTime\":1712363738,\"meta\":{\"computeUnitsConsumed\":6173,\"err\":null,\"fee\":5000,\"innerInstructions\":[],\"loadedAddresses\":{\"readonly\":[],\"writable\":[]},\"logMessages\":[\"Program TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA invoke [1]\",\"Program log: Instruction: TransferChecked\",\"Program TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA consumed 6173 of 200000 compute units\",\"Program TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA success\"],\"postBalances\":[997945720,2039280,2039280,929020800,1461600],\"postTokenBalances\":[{\"accountIndex\":1,\"mint\":\"FYU5Uxh5mZn8VPLvUq24khhSWK1BN9gXUMG1Gx7RDa1H\",\"owner\":\"FkdZH693o3s77CVr72hgyaP4LURXqLVnYF69kMB3sFX2\",\"programId\":\"TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA\",\"uiTokenAmount\":{\"amount\":\"244803289765\",\"decimals\":9,\"uiAmount\":244.803289765,\"uiAmountString\":\"244.803289765\"}},{\"accountIndex\":2,\"mint\":\"FYU5Uxh5mZn8VPLvUq24khhSWK1BN9gXUMG1Gx7RDa1H\",\"owner\":\"GHxPmQcC4s6iGi9bAFtsw75wB2RprqXGXbEewTcN7EyN\",\"programId\":\"TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA\",\"uiTokenAmount\":{\"amount\":\"855196710235\",\"decimals\":9,\"uiAmount\":855.196710235,\"uiAmountString\":\"855.196710235\"}}],\"preBalances\":[997950720,2039280,2039280,929020800,1461600],\"preTokenBalances\":[{\"accountIndex\":1,\"mint\":\"FYU5Uxh5mZn8VPLvUq24khhSWK1BN9gXUMG1Gx7RDa1H\",\"owner\":\"FkdZH693o3s77CVr72hgyaP4LURXqLVnYF69kMB3sFX2\",\"programId\":\"TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA\",\"uiTokenAmount\":{\"amount\":\"254803289765\",\"decimals\":9,\"uiAmount\":254.803289765,\"uiAmountString\":\"254.803289765\"}},{\"accountIndex\":2,\"mint\":\"FYU5Uxh5mZn8VPLvUq24khhSWK1BN9gXUMG1Gx7RDa1H\",\"owner\":\"GHxPmQcC4s6iGi9bAFtsw75wB2RprqXGXbEewTcN7EyN\",\"programId\":\"TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA\",\"uiTokenAmount\":{\"amount\":\"845196710235\",\"decimals\":9,\"uiAmount\":845.196710235,\"uiAmountString\":\"845.196710235\"}}],\"rewards\":[],\"status\":{\"Ok\":null}},\"slot\":81965,\"transaction\":{\"message\":{\"accountKeys\":[\"FkdZH693o3s77CVr72hgyaP4LURXqLVnYF69kMB3sFX2\",\"5sQvEQfJGZgfoRoGdqaHS1bEudiAYToaUjxdb4dHLaym\",\"HJ5z5cmD76tAN1kxSWuvuAxwmtpaXxGvHxEbF2FBAinY\",\"TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA\",\"FYU5Uxh5mZn8VPLvUq24khhSWK1BN9gXUMG1Gx7RDa1H\"],\"header\":{\"numReadonlySignedAccounts\":0,\"numReadonlyUnsignedAccounts\":2,\"numRequiredSignatures\":1},\"instructions\":[{\"accounts\":[1,4,2,0],\"data\":\"g7c6qhYoikLGp\",\"programIdIndex\":3,\"stackHeight\":null}],\"recentBlockhash\":\"BmV8JtjyeE556qhKPLfcieqAXP3VvjmrB4soCuwmM2Cq\"},\"signatures\":[\"3YJbniPs66oJN5yGDLkhyGRdQiVk7p5TgtZzc7j5e2T8ypkcfhbH5qowCraUKrj7X7n4QD6gGPkJLGX5WCoN6KZZ\"]}},\"id\":1}\r\n";

        private HttpResponseMessage GetHttpResponseMessage()
            => new HttpResponseMessage()
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(TransactionJson, Encoding.UTF8),
                ReasonPhrase = "Test"
            };

        private TransactionMetaSlotInfo MockGetTransactionResult()
            => JsonConvert.DeserializeObject<RequestResult<TransactionMetaSlotInfo>>(TransactionJson).Result;
    }
}
