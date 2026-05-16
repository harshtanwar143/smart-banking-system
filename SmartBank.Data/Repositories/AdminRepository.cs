using Microsoft.EntityFrameworkCore;
using SmartBank.Data.Context;
using SmartBank.Models.Entities;

namespace SmartBank.Data.Repositories;

public interface IAdminRepository
{
    Task<List<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(int userId);
    Task<List<Account>> GetAccountsByUserAsync(int userId);
    Task UpdateUserAsync(User user);
    Task UpdateAccountsFreezeAsync(int userId, bool freeze);

    Task<int> GetTotalUserCountAsync();
    Task<int> GetTotalAccountCountAsync();
    Task<decimal> GetTotalDepositsAsync();
    Task<int> GetTodayTransactionCountAsync();
    Task<decimal> GetTodayTransactionVolumeAsync();
    Task<int> GetPendingLoanCountAsync();
    Task<int> GetOpenTicketCountAsync();
    Task<int> GetFrozenUserCountAsync();
    Task<List<(DateTime Date, int Count, decimal Volume)>> GetLast7DaysSummaryAsync();
    Task<List<Account>> GetTopAccountsAsync(int top);
    Task<List<Account>> GetLowBalanceAccountsAsync();
}

public class AdminRepository : IAdminRepository
{
    private readonly SmartOnlineBankingDbContext _context;

    public AdminRepository(SmartOnlineBankingDbContext context)
    {
        _context = context;
    }

    public async Task<List<User>> GetAllUsersAsync()
        => await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Accounts)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

    public async Task<User?> GetUserByIdAsync(int userId)
        => await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Accounts)
            .FirstOrDefaultAsync(u => u.UserId == userId);

    public async Task<List<Account>> GetAccountsByUserAsync(int userId)
        => await _context.Accounts.Where(a => a.UserId == userId).ToListAsync();

    public async Task UpdateUserAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAccountsFreezeAsync(int userId, bool freeze)
    {
        var newStatus = freeze ? "Frozen" : "Active";
        var accounts  = await _context.Accounts.Where(a => a.UserId == userId).ToListAsync();
        foreach (var a in accounts)
        {
            a.Status    = newStatus;
            a.UpdatedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
    }

    public Task<int> GetTotalUserCountAsync()
        => _context.Users.Where(u => u.Role.RoleName == "Customer").CountAsync();

    public Task<int> GetTotalAccountCountAsync()
        => _context.Accounts.CountAsync();

    public async Task<decimal> GetTotalDepositsAsync()
        => await _context.Accounts.Where(a => a.Status == "Active").SumAsync(a => (decimal?)a.Balance) ?? 0;

    public Task<int> GetTodayTransactionCountAsync()
    {
        var today = DateTime.UtcNow.Date;
        return _context.Transactions
            .Where(t => t.CreatedAt != null && t.CreatedAt.Value.Date == today)
            .CountAsync();
    }

    public async Task<decimal> GetTodayTransactionVolumeAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await _context.Transactions
            .Where(t => t.CreatedAt != null && t.CreatedAt.Value.Date == today)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;
    }

    public Task<int> GetPendingLoanCountAsync()
        => _context.Loans.Where(l => l.Status == "Pending").CountAsync();

    public Task<int> GetOpenTicketCountAsync()
        => _context.SupportTickets.Where(t => t.Status != "Resolved" && t.Status != "Closed").CountAsync();

    public Task<int> GetFrozenUserCountAsync()
        => _context.Users.Where(u => u.IsFrozen == true).CountAsync();

    public async Task<List<(DateTime Date, int Count, decimal Volume)>> GetLast7DaysSummaryAsync()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-6);

        var rows = await _context.Transactions
            .Where(t => t.CreatedAt != null && t.CreatedAt.Value.Date >= startDate)
            .GroupBy(t => t.CreatedAt!.Value.Date)
            .Select(g => new { Date = g.Key, Count = g.Count(), Volume = g.Sum(x => x.Amount) })
            .ToListAsync();

        // Fill missing days with zero
        var result = new List<(DateTime Date, int Count, decimal Volume)>();
        for (var d = startDate; d <= DateTime.UtcNow.Date; d = d.AddDays(1))
        {
            var match = rows.FirstOrDefault(r => r.Date == d);
            result.Add((d, match?.Count ?? 0, match?.Volume ?? 0));
        }
        return result;
    }

    public async Task<List<Account>> GetTopAccountsAsync(int top)
        => await _context.Accounts
            .Include(a => a.User)
            .Where(a => a.Status == "Active")
            .OrderByDescending(a => a.Balance)
            .Take(top)
            .ToListAsync();

    public async Task<List<Account>> GetLowBalanceAccountsAsync()
        => await _context.Accounts
            .Include(a => a.User)
            .Where(a => a.Status == "Active" && a.Balance <= (a.MinimumBalance ?? 500m) * 1.1m)
            .OrderBy(a => a.Balance)
            .ToListAsync();
}
