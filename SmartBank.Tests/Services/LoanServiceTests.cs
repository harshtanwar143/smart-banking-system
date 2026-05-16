using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmartBank.API.Services;
using SmartBank.Data.Repositories;
using SmartBank.Models.DTOs.Loans;
using SmartBank.Models.Entities;
using Xunit;

namespace SmartBank.Tests.Services;

public class LoanServiceTests
{
    private readonly Mock<ILoanRepository> _loanRepo;
    private readonly Mock<INotificationRepository> _notifRepo;
    private readonly Mock<ILogger<LoanService>> _logger;
    private readonly LoanService _service;

    public LoanServiceTests()
    {
        _loanRepo  = new Mock<ILoanRepository>();
        _notifRepo = new Mock<INotificationRepository>();
        _logger    = new Mock<ILogger<LoanService>>();
        _service   = new LoanService(_loanRepo.Object, _notifRepo.Object, _logger.Object);
    }

    [Fact]
    public async Task Apply_ValidLoan_ShouldSucceed()
    {
        _loanRepo.Setup(r => r.AddLoanAsync(It.IsAny<Loan>()))
            .ReturnsAsync((Loan l) => { l.LoanId = 1; return l; });
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<Notification>()))
            .ReturnsAsync((Notification n) => { n.NotificationId = 1; return n; });

        var result = await _service.ApplyAsync(1, new LoanApplicationDto
        {
            LoanType        = "Personal",
            RequestedAmount = 100_000m,
            TenureMonths    = 24,
            Purpose         = "Home renovation"
        });

        result.Success.Should().BeTrue();
        result.Loan.Should().NotBeNull();
        result.Loan!.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Apply_InvalidLoanType_ShouldFail()
    {
        var result = await _service.ApplyAsync(1, new LoanApplicationDto
        {
            LoanType        = "CryptoLoan",
            RequestedAmount = 50_000m,
            TenureMonths    = 12,
            Purpose         = "Buying crypto"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid loan type");
    }

    [Fact]
    public async Task Review_ApproveLoan_ShouldSetEmi()
    {
        var loan = new Loan
        {
            LoanId          = 1,
            UserId          = 10,
            LoanType        = "Personal",
            RequestedAmount = 100_000m,
            TenureMonths    = 12,
            Status          = "Pending",
            User            = new User { UserId = 10, FirstName = "A", LastName = "B" }
        };

        _loanRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(loan);
        _loanRepo.Setup(r => r.UpdateLoanAsync(It.IsAny<Loan>())).Returns(Task.CompletedTask);
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<Notification>()))
            .ReturnsAsync((Notification n) => { n.NotificationId = 1; return n; });

        var result = await _service.ReviewAsync(99, new LoanReviewDto
        {
            LoanId         = 1,
            Approve        = true,
            ApprovedAmount = 90_000m,
            InterestRate   = 10.5m
        });

        result.Success.Should().BeTrue();
        result.Loan!.Status.Should().Be("Approved");
        result.Loan.ApprovedAmount.Should().Be(90_000m);
        result.Loan.EmiAmount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Review_RejectLoan_ShouldSetReason()
    {
        var loan = new Loan
        {
            LoanId          = 2,
            UserId          = 10,
            LoanType        = "Home",
            RequestedAmount = 5_000_000m,
            TenureMonths    = 240,
            Status          = "Pending",
            User            = new User { UserId = 10, FirstName = "A", LastName = "B" }
        };

        _loanRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(loan);
        _loanRepo.Setup(r => r.UpdateLoanAsync(It.IsAny<Loan>())).Returns(Task.CompletedTask);
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<Notification>()))
            .ReturnsAsync((Notification n) => { n.NotificationId = 1; return n; });

        var result = await _service.ReviewAsync(99, new LoanReviewDto
        {
            LoanId          = 2,
            Approve         = false,
            RejectionReason = "Insufficient income documentation"
        });

        result.Success.Should().BeTrue();
        result.Loan!.Status.Should().Be("Rejected");
        result.Loan.RejectionReason.Should().Be("Insufficient income documentation");
    }

    [Fact]
    public async Task Review_AlreadyApprovedLoan_ShouldFail()
    {
        var loan = new Loan { LoanId = 3, Status = "Approved", UserId = 10 };
        _loanRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(loan);

        var result = await _service.ReviewAsync(99, new LoanReviewDto { LoanId = 3, Approve = true });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already");
    }
}
