using System.ComponentModel.DataAnnotations;

namespace SmartBank.Models.DTOs.Admin;

public class AdminUserDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = null!;
    public string KycStatus { get; set; } = "Pending";
    public bool IsActive { get; set; }
    public bool IsFrozen { get; set; }
    public int AccountCount { get; set; }
    public decimal TotalBalance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class AdminUserListDto
{
    public List<AdminUserDto> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int FrozenCount { get; set; }
}

public class FreezeUserDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public bool Freeze { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}

public class AdminActionResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalAccounts { get; set; }
    public decimal TotalDeposits { get; set; }
    public int TodayTransactions { get; set; }
    public decimal TodayVolume { get; set; }
    public int PendingLoans { get; set; }
    public int OpenTickets { get; set; }
    public int FrozenAccounts { get; set; }
    public List<DailyTransactionPointDto> Last7Days { get; set; } = new();
    public List<TopAccountDto> TopAccounts { get; set; } = new();
}

public class DailyTransactionPointDto
{
    public string Date { get; set; } = null!;
    public int Count { get; set; }
    public decimal Volume { get; set; }
}

public class TopAccountDto
{
    public string AccountNumber { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public decimal Balance { get; set; }
    public string AccountType { get; set; } = null!;
}

public class ReportsDto
{
    public DashboardStatsDto Stats { get; set; } = new();
    public List<LowBalanceAccountDto> LowBalanceAccounts { get; set; } = new();
    public List<DailyTransactionPointDto> DailySummary { get; set; } = new();
}

public class LowBalanceAccountDto
{
    public string AccountNumber { get; set; } = null!;
    public string AccountType { get; set; } = null!;
    public decimal Balance { get; set; }
    public decimal MinimumBalance { get; set; }
    public string CustomerName { get; set; } = null!;
    public string? Email { get; set; }
}
