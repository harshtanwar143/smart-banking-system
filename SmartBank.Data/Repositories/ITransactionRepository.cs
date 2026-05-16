using SmartBank.Models.Entities;

namespace SmartBank.Data.Repositories;

public interface ITransactionRepository
{
    Task<Account?> GetAccountAsync(int accountId);
    Task<Account?> GetAccountByNumberAsync(string accountNumber);
    Task<bool> AccountBelongsToUserAsync(int accountId, int userId);

    Task<Transaction> AddTransactionAsync(Transaction transaction);
    Task<Transfer> AddTransferAsync(Transfer transfer);
    Task UpdateAccountAsync(Account account);
    Task UpdateTransferAsync(Transfer transfer);

    Task<List<Transaction>> GetHistoryAsync(int accountId, int skip, int take);
    Task<int> GetHistoryCountAsync(int accountId);
    Task<List<Transaction>> GetUserHistoryAsync(int userId, int skip, int take);
    Task<int> GetUserHistoryCountAsync(int userId);

    /// <summary>Begin a database transaction for atomic transfer operations.</summary>
    Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync();
    Task SaveChangesAsync();
}
