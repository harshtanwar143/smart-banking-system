using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SmartBank.Data.Context;
using SmartBank.Models.Entities;

namespace SmartBank.Data.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly SmartOnlineBankingDbContext _context;

    public TransactionRepository(SmartOnlineBankingDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetAccountAsync(int accountId)
        => await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == accountId);

    public async Task<Account?> GetAccountByNumberAsync(string accountNumber)
        => await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);

    public async Task<bool> AccountBelongsToUserAsync(int accountId, int userId)
        => await _context.Accounts.AnyAsync(a => a.AccountId == accountId && a.UserId == userId);

    public async Task<Transaction> AddTransactionAsync(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<Transfer> AddTransferAsync(Transfer transfer)
    {
        _context.Transfers.Add(transfer);
        await _context.SaveChangesAsync();
        return transfer;
    }

    public async Task UpdateAccountAsync(Account account)
    {
        account.UpdatedAt = DateTime.UtcNow;
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateTransferAsync(Transfer transfer)
    {
        _context.Transfers.Update(transfer);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Transaction>> GetHistoryAsync(int accountId, int skip, int take)
        => await _context.Transactions
            .Include(t => t.Account)
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip).Take(take)
            .ToListAsync();

    public async Task<int> GetHistoryCountAsync(int accountId)
        => await _context.Transactions.CountAsync(t => t.AccountId == accountId);

    public async Task<List<Transaction>> GetUserHistoryAsync(int userId, int skip, int take)
        => await _context.Transactions
            .Include(t => t.Account)
            .Where(t => t.Account.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip).Take(take)
            .ToListAsync();

    public async Task<int> GetUserHistoryCountAsync(int userId)
        => await _context.Transactions.CountAsync(t => t.Account.UserId == userId);

    public Task<IDbContextTransaction> BeginTransactionAsync()
        => _context.Database.BeginTransactionAsync();

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
