using SmartBank.Data.Repositories;
using SmartBank.Models.DTOs.Admin;
using SmartBank.Models.Entities;

namespace SmartBank.API.Services;

public interface IAdminService
{
    Task<AdminUserListDto> GetAllUsersAsync();
    Task<AdminActionResultDto> FreezeUserAsync(int adminUserId, FreezeUserDto request);
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    Task<ReportsDto> GetReportsAsync();
}

public class AdminService : IAdminService
{
    private readonly IAdminRepository _repo;
    private readonly INotificationRepository _notif;
    private readonly ILogger<AdminService> _logger;

    public AdminService(IAdminRepository repo, INotificationRepository notif, ILogger<AdminService> logger)
    {
        _repo   = repo;
        _notif  = notif;
        _logger = logger;
    }

    public async Task<AdminUserListDto> GetAllUsersAsync()
    {
        var users = await _repo.GetAllUsersAsync();
        var dtos  = users.Select(MapUser).ToList();

        return new AdminUserListDto
        {
            Users        = dtos,
            TotalCount   = dtos.Count,
            ActiveCount  = dtos.Count(u => u.IsActive && !u.IsFrozen),
            FrozenCount  = dtos.Count(u => u.IsFrozen)
        };
    }

    public async Task<AdminActionResultDto> FreezeUserAsync(int adminUserId, FreezeUserDto request)
    {
        var user = await _repo.GetUserByIdAsync(request.UserId);
        if (user is null)
            return new AdminActionResultDto { Success = false, Message = "User not found." };

        if (adminUserId == user.UserId)
            return new AdminActionResultDto { Success = false, Message = "You cannot freeze your own account." };

        user.IsFrozen = request.Freeze;
        await _repo.UpdateUserAsync(user);
        await _repo.UpdateAccountsFreezeAsync(user.UserId, request.Freeze);

        var action = request.Freeze ? "frozen" : "unfrozen";
        await _notif.AddAsync(new Notification
        {
            UserId            = user.UserId,
            Title             = $"Account {action}",
            Message           = $"Your account has been {action} by an administrator." +
                                (string.IsNullOrEmpty(request.Reason) ? "" : $" Reason: {request.Reason}"),
            Type              = "Admin",
            CreatedAt         = DateTime.UtcNow
        });

        _logger.LogInformation("Admin {AdminId} {Action} user {UserId}", adminUserId, action, user.UserId);

        return new AdminActionResultDto
        {
            Success = true,
            Message = $"User account {action} successfully."
        };
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var last7  = await _repo.GetLast7DaysSummaryAsync();
        var topAcc = await _repo.GetTopAccountsAsync(5);

        return new DashboardStatsDto
        {
            TotalUsers        = await _repo.GetTotalUserCountAsync(),
            TotalAccounts     = await _repo.GetTotalAccountCountAsync(),
            TotalDeposits     = await _repo.GetTotalDepositsAsync(),
            TodayTransactions = await _repo.GetTodayTransactionCountAsync(),
            TodayVolume       = await _repo.GetTodayTransactionVolumeAsync(),
            PendingLoans      = await _repo.GetPendingLoanCountAsync(),
            OpenTickets       = await _repo.GetOpenTicketCountAsync(),
            FrozenAccounts    = await _repo.GetFrozenUserCountAsync(),
            Last7Days         = last7.Select(x => new DailyTransactionPointDto
            {
                Date   = x.Date.ToString("dd MMM"),
                Count  = x.Count,
                Volume = x.Volume
            }).ToList(),
            TopAccounts       = topAcc.Select(a => new TopAccountDto
            {
                AccountNumber = a.AccountNumber,
                CustomerName  = $"{a.User.FirstName} {a.User.LastName}",
                Balance       = a.Balance,
                AccountType   = a.AccountType
            }).ToList()
        };
    }

    public async Task<ReportsDto> GetReportsAsync()
    {
        var stats = await GetDashboardStatsAsync();
        var lowBal = await _repo.GetLowBalanceAccountsAsync();

        return new ReportsDto
        {
            Stats              = stats,
            DailySummary       = stats.Last7Days,
            LowBalanceAccounts = lowBal.Select(a => new LowBalanceAccountDto
            {
                AccountNumber  = a.AccountNumber,
                AccountType    = a.AccountType,
                Balance        = a.Balance,
                MinimumBalance = a.MinimumBalance ?? 500m,
                CustomerName   = $"{a.User.FirstName} {a.User.LastName}",
                Email          = a.User.Email
            }).ToList()
        };
    }

    private static AdminUserDto MapUser(User u) => new()
    {
        UserId       = u.UserId,
        FirstName    = u.FirstName,
        LastName     = u.LastName,
        Email        = u.Email,
        PhoneNumber  = u.PhoneNumber,
        Role         = u.Role?.RoleName ?? "Customer",
        KycStatus    = u.KycStatus ?? "Pending",
        IsActive     = u.IsActive ?? true,
        IsFrozen     = u.IsFrozen ?? false,
        AccountCount = u.Accounts.Count,
        TotalBalance = u.Accounts.Sum(a => a.Balance),
        CreatedAt    = u.CreatedAt ?? DateTime.UtcNow,
        LastLoginAt  = u.LastLoginAt
    };
}
