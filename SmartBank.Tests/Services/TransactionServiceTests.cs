using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using SmartBank.API.Services;
using SmartBank.Data.Repositories;
using SmartBank.Models.DTOs.Transactions;
using SmartBank.Models.Entities;
using Xunit;

namespace SmartBank.Tests.Services;

public class TransactionServiceTests
{
    private readonly Mock<ITransactionRepository> _repo;
    private readonly Mock<ILogger<TransactionService>> _logger;
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        _repo   = new Mock<ITransactionRepository>();
        _logger = new Mock<ILogger<TransactionService>>();
        _service = new TransactionService(_repo.Object, _logger.Object);
    }

    // ─── Deposit Tests ────────────────────────────────────────────────────────

    [Fact]
    public async Task Deposit_ValidRequest_ShouldIncrementBalance()
    {
        var account = MakeAccount(1, 1, 1000m, "Active");
        _repo.Setup(r => r.GetAccountAsync(1)).ReturnsAsync(account);
        _repo.Setup(r => r.UpdateAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.AddTransactionAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) => { t.TransactionId = 1; return t; });

        var result = await _service.DepositAsync(1, new DepositRequestDto { AccountId = 1, Amount = 500m });

        result.Success.Should().BeTrue();
        result.NewBalance.Should().Be(1500m);
    }

    [Fact]
    public async Task Deposit_ZeroAmount_ShouldFail()
    {
        var result = await _service.DepositAsync(1, new DepositRequestDto { AccountId = 1, Amount = 0 });
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Deposit_FrozenAccount_ShouldFail()
    {
        var account = MakeAccount(1, 1, 1000m, "Frozen");
        _repo.Setup(r => r.GetAccountAsync(1)).ReturnsAsync(account);

        var result = await _service.DepositAsync(1, new DepositRequestDto { AccountId = 1, Amount = 100m });
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("frozen");
    }

    [Fact]
    public async Task Deposit_OtherUsersAccount_ShouldFail()
    {
        var account = MakeAccount(1, userId: 999, 1000m, "Active");
        _repo.Setup(r => r.GetAccountAsync(1)).ReturnsAsync(account);

        var result = await _service.DepositAsync(1, new DepositRequestDto { AccountId = 1, Amount = 100m });
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("own");
    }

    // ─── Withdraw Tests ───────────────────────────────────────────────────────

    [Fact]
    public async Task Withdraw_ValidAmount_ShouldDecrementBalance()
    {
        var account = MakeAccount(1, 1, 5000m, "Active", 500m);
        _repo.Setup(r => r.GetAccountAsync(1)).ReturnsAsync(account);
        _repo.Setup(r => r.UpdateAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.AddTransactionAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) => { t.TransactionId = 2; return t; });

        var result = await _service.WithdrawAsync(1, new WithdrawRequestDto { AccountId = 1, Amount = 2000m });

        result.Success.Should().BeTrue();
        result.NewBalance.Should().Be(3000m);
    }

    [Fact]
    public async Task Withdraw_BelowMinBalance_ShouldFail()
    {
        var account = MakeAccount(1, 1, 1000m, "Active", 500m);
        _repo.Setup(r => r.GetAccountAsync(1)).ReturnsAsync(account);

        var result = await _service.WithdrawAsync(1, new WithdrawRequestDto { AccountId = 1, Amount = 600m });
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Insufficient");
    }

    // ─── Transfer Tests ───────────────────────────────────────────────────────

    [Fact]
    public async Task Transfer_ValidTransfer_ShouldMoveFunds()
    {
        var fromAccount = MakeAccount(1, 1, 5000m, "Active", 500m);
        var toAccount   = MakeAccount(2, 2, 1000m, "Active", accountNumber: "SB0000000002");

        _repo.Setup(r => r.GetAccountAsync(1)).ReturnsAsync(fromAccount);
        _repo.Setup(r => r.GetAccountByNumberAsync("SB0000000002")).ReturnsAsync(toAccount);
        _repo.Setup(r => r.UpdateAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.AddTransferAsync(It.IsAny<Transfer>()))
            .ReturnsAsync((Transfer t) => { t.TransferId = 1; return t; });
        _repo.Setup(r => r.UpdateTransferAsync(It.IsAny<Transfer>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.AddTransactionAsync(It.IsAny<Transaction>()))
            .ReturnsAsync((Transaction t) => { t.TransactionId = 1; return t; });

        var mockDbTxn = new Mock<IDbContextTransaction>();
        _repo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(mockDbTxn.Object);

        var result = await _service.TransferAsync(1, new TransferRequestDto
        {
            FromAccountId   = 1,
            ToAccountNumber = "SB0000000002",
            Amount          = 1000m,
            Remarks         = "Test"
        });

        result.Success.Should().BeTrue();
        result.NewBalance.Should().Be(4000m);
        result.ToAccountNumber.Should().Be("SB0000000002");
        mockDbTxn.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Transfer_ToSameAccount_ShouldFail()
    {
        var account = MakeAccount(1, 1, 5000m, "Active", accountNumber: "SB0000000001");
        _repo.Setup(r => r.GetAccountAsync(1)).ReturnsAsync(account);
        _repo.Setup(r => r.GetAccountByNumberAsync("SB0000000001")).ReturnsAsync(account);

        var result = await _service.TransferAsync(1, new TransferRequestDto
        {
            FromAccountId   = 1,
            ToAccountNumber = "SB0000000001",
            Amount          = 100m
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("same account");
    }

    [Fact]
    public async Task Transfer_InsufficientFunds_ShouldFail()
    {
        var fromAccount = MakeAccount(1, 1, 1000m, "Active", 500m);
        var toAccount   = MakeAccount(2, 2, 500m, "Active", accountNumber: "SB0000000002");

        _repo.Setup(r => r.GetAccountAsync(1)).ReturnsAsync(fromAccount);
        _repo.Setup(r => r.GetAccountByNumberAsync("SB0000000002")).ReturnsAsync(toAccount);

        var result = await _service.TransferAsync(1, new TransferRequestDto
        {
            FromAccountId   = 1,
            ToAccountNumber = "SB0000000002",
            Amount          = 600m
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Insufficient");
    }

    // ─── History Tests ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetHistory_ReturnsPagedResults()
    {
        _repo.Setup(r => r.AccountBelongsToUserAsync(1, 1)).ReturnsAsync(true);
        _repo.Setup(r => r.GetHistoryCountAsync(1)).ReturnsAsync(50);
        _repo.Setup(r => r.GetHistoryAsync(1, 0, 20))
            .ReturnsAsync(Enumerable.Range(1, 20).Select(i => new Transaction
            {
                TransactionId = i,
                AccountId     = 1,
                Amount        = 100m * i,
                BalanceAfter  = 1000m + 100m * i,
                TransactionType = "Deposit",
                CreatedAt     = DateTime.UtcNow.AddMinutes(-i),
                Account       = new Account { AccountNumber = "SB0000000001" }
            }).ToList());

        var result = await _service.GetHistoryAsync(1, 1, 1, 20);

        result.TotalCount.Should().Be(50);
        result.Page.Should().Be(1);
        result.Transactions.Should().HaveCount(20);
        result.TotalPages.Should().Be(3);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static Account MakeAccount(int accountId, int userId, decimal balance, string status,
        decimal minBalance = 0, string accountNumber = "SB0000000001", string accountType = "Savings")
        => new()
        {
            AccountId      = accountId,
            UserId         = userId,
            Balance        = balance,
            Status         = status,
            MinimumBalance = minBalance,
            AccountNumber  = accountNumber,
            AccountType    = accountType,
            User           = new User { UserId = userId, FirstName = "Test", LastName = "User" }
        };
}
