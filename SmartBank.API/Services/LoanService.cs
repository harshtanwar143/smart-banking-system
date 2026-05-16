using SmartBank.Data.Repositories;
using SmartBank.Models.DTOs.Loans;
using SmartBank.Models.Entities;

namespace SmartBank.API.Services;

public interface ILoanService
{
    Task<LoanApplyResultDto> ApplyAsync(int userId, LoanApplicationDto request);
    Task<LoanListDto> GetMyLoansAsync(int userId);
    Task<LoanListDto> GetAllLoansAsync();
    Task<LoanListDto> GetPendingLoansAsync();
    Task<LoanApplyResultDto> ReviewAsync(int reviewerUserId, LoanReviewDto request);
}

public class LoanService : ILoanService
{
    private readonly ILoanRepository _repo;
    private readonly INotificationRepository _notif;
    private readonly ILogger<LoanService> _logger;

    public LoanService(ILoanRepository repo, INotificationRepository notif, ILogger<LoanService> logger)
    {
        _repo   = repo;
        _notif  = notif;
        _logger = logger;
    }

    public async Task<LoanApplyResultDto> ApplyAsync(int userId, LoanApplicationDto request)
    {
        var validTypes = new[] { "Personal", "Home", "Vehicle", "Education", "Business" };
        if (!validTypes.Contains(request.LoanType, StringComparer.OrdinalIgnoreCase))
            return new LoanApplyResultDto { Success = false, Message = "Invalid loan type." };

        var loan = new Loan
        {
            UserId          = userId,
            LoanType        = request.LoanType,
            RequestedAmount = request.RequestedAmount,
            TenureMonths    = request.TenureMonths,
            Purpose         = request.Purpose,
            Status          = "Pending",
            CreatedAt       = DateTime.UtcNow
        };

        await _repo.AddLoanAsync(loan);

        await _notif.AddAsync(new Notification
        {
            UserId            = userId,
            Title             = "Loan Application Received",
            Message           = $"Your {request.LoanType} loan application for INR {request.RequestedAmount:N2} has been received and is under review.",
            Type              = "Loan",
            RelatedEntityId   = loan.LoanId,
            RelatedEntityType = "Loan",
            CreatedAt         = DateTime.UtcNow
        });

        _logger.LogInformation("Loan {LoanId} applied by user {UserId}: {Type} INR {Amount}",
            loan.LoanId, userId, request.LoanType, request.RequestedAmount);

        return new LoanApplyResultDto
        {
            Success = true,
            Message = "Loan application submitted successfully. You will be notified when reviewed.",
            Loan    = MapLoan(loan)
        };
    }

    public async Task<LoanListDto> GetMyLoansAsync(int userId)
    {
        var loans = await _repo.GetByUserAsync(userId);
        return new LoanListDto
        {
            Loans      = loans.Select(MapLoan).ToList(),
            TotalCount = loans.Count
        };
    }

    public async Task<LoanListDto> GetAllLoansAsync()
    {
        var loans = await _repo.GetAllAsync();
        return new LoanListDto
        {
            Loans      = loans.Select(MapLoan).ToList(),
            TotalCount = loans.Count
        };
    }

    public async Task<LoanListDto> GetPendingLoansAsync()
    {
        var loans = await _repo.GetAllPendingAsync();
        return new LoanListDto
        {
            Loans      = loans.Select(MapLoan).ToList(),
            TotalCount = loans.Count
        };
    }

    public async Task<LoanApplyResultDto> ReviewAsync(int reviewerUserId, LoanReviewDto request)
    {
        var loan = await _repo.GetByIdAsync(request.LoanId);
        if (loan is null)
            return new LoanApplyResultDto { Success = false, Message = "Loan not found." };

        if (loan.Status != "Pending")
            return new LoanApplyResultDto { Success = false, Message = $"Loan is already {loan.Status?.ToLower()}." };

        loan.ReviewedByUserId = reviewerUserId;
        loan.ReviewedAt       = DateTime.UtcNow;

        if (request.Approve)
        {
            var approvedAmt = request.ApprovedAmount ?? loan.RequestedAmount;
            var rate        = request.InterestRate    ?? 10.5m; // default 10.5% p.a.

            loan.ApprovedAmount = approvedAmt;
            loan.InterestRate   = rate;
            loan.Status         = "Approved";
            loan.Emiamount      = CalculateEmi(approvedAmt, rate, loan.TenureMonths);

            await _repo.UpdateLoanAsync(loan);

            await _notif.AddAsync(new Notification
            {
                UserId            = loan.UserId,
                Title             = "Loan Approved",
                Message           = $"Your {loan.LoanType} loan of INR {approvedAmt:N2} has been approved at {rate}% p.a. EMI: INR {loan.Emiamount:N2}/month.",
                Type              = "Loan",
                RelatedEntityId   = loan.LoanId,
                RelatedEntityType = "Loan",
                CreatedAt         = DateTime.UtcNow
            });

            _logger.LogInformation("Loan {LoanId} approved by reviewer {ReviewerId}", loan.LoanId, reviewerUserId);
        }
        else
        {
            loan.Status          = "Rejected";
            loan.RejectionReason = request.RejectionReason ?? "Application did not meet eligibility criteria.";

            await _repo.UpdateLoanAsync(loan);

            await _notif.AddAsync(new Notification
            {
                UserId            = loan.UserId,
                Title             = "Loan Rejected",
                Message           = $"Your {loan.LoanType} loan application was rejected. Reason: {loan.RejectionReason}",
                Type              = "Loan",
                RelatedEntityId   = loan.LoanId,
                RelatedEntityType = "Loan",
                CreatedAt         = DateTime.UtcNow
            });

            _logger.LogInformation("Loan {LoanId} rejected by reviewer {ReviewerId}", loan.LoanId, reviewerUserId);
        }

        return new LoanApplyResultDto
        {
            Success = true,
            Message = request.Approve ? "Loan approved." : "Loan rejected.",
            Loan    = MapLoan(loan)
        };
    }

    private static decimal CalculateEmi(decimal principal, decimal annualRate, int months)
    {
        var monthlyRate = (double)(annualRate / 100m / 12m);
        var p = (double)principal;
        var n = months;
        if (monthlyRate <= 0) return Math.Round(principal / months, 2);
        var emi = p * monthlyRate * Math.Pow(1 + monthlyRate, n) / (Math.Pow(1 + monthlyRate, n) - 1);
        return Math.Round((decimal)emi, 2);
    }

    private static LoanResponseDto MapLoan(Loan l) => new()
    {
        LoanId          = l.LoanId,
        LoanType        = l.LoanType,
        RequestedAmount = l.RequestedAmount,
        ApprovedAmount  = l.ApprovedAmount,
        InterestRate    = l.InterestRate,
        TenureMonths    = l.TenureMonths,
        EmiAmount       = l.Emiamount,
        Purpose         = l.Purpose,
        Status          = l.Status ?? "Pending",
        RejectionReason = l.RejectionReason,
        CreatedAt       = l.CreatedAt ?? DateTime.UtcNow,
        ReviewedAt      = l.ReviewedAt,
        ReviewedBy      = l.ReviewedByUser is null ? null
                          : $"{l.ReviewedByUser.FirstName} {l.ReviewedByUser.LastName}"
    };
}
