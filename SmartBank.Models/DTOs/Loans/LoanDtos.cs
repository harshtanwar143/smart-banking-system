using System.ComponentModel.DataAnnotations;

namespace SmartBank.Models.DTOs.Loans;

public class LoanApplicationDto
{
    [Required]
    [MaxLength(50)]
    public string LoanType { get; set; } = null!; // Personal, Home, Vehicle, Education, Business

    [Required]
    [Range(10000, 10_000_000, ErrorMessage = "Loan amount must be between 10,000 and 1,00,00,000.")]
    public decimal RequestedAmount { get; set; }

    [Required]
    [Range(6, 360, ErrorMessage = "Tenure must be between 6 and 360 months.")]
    public int TenureMonths { get; set; }

    [Required]
    [MaxLength(500)]
    public string Purpose { get; set; } = null!;
}

public class LoanResponseDto
{
    public int LoanId { get; set; }
    public string? LoanType { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public decimal? InterestRate { get; set; }
    public int TenureMonths { get; set; }
    public decimal? EmiAmount { get; set; }
    public string? Purpose { get; set; }
    public string Status { get; set; } = "Pending";
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
}

public class LoanApplyResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public LoanResponseDto? Loan { get; set; }
}

public class LoanListDto
{
    public List<LoanResponseDto> Loans { get; set; } = new();
    public int TotalCount { get; set; }
}

// ─── Admin loan review ───────────────────────────────────────────────────────

public class LoanReviewDto
{
    [Required]
    public int LoanId { get; set; }

    [Required]
    public bool Approve { get; set; }

    public decimal? ApprovedAmount { get; set; }

    [Range(0, 50)]
    public decimal? InterestRate { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }
}
