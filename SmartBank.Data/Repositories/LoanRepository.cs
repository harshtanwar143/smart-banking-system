using Microsoft.EntityFrameworkCore;
using SmartBank.Data.Context;
using SmartBank.Models.Entities;

namespace SmartBank.Data.Repositories;

public interface ILoanRepository
{
    Task<Loan> AddLoanAsync(Loan loan);
    Task<Loan?> GetByIdAsync(int loanId);
    Task<List<Loan>> GetByUserAsync(int userId);
    Task<List<Loan>> GetAllPendingAsync();
    Task<List<Loan>> GetAllAsync();
    Task UpdateLoanAsync(Loan loan);
}

public class LoanRepository : ILoanRepository
{
    private readonly SmartOnlineBankingDbContext _context;

    public LoanRepository(SmartOnlineBankingDbContext context)
    {
        _context = context;
    }

    public async Task<Loan> AddLoanAsync(Loan loan)
    {
        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();
        return loan;
    }

    public async Task<Loan?> GetByIdAsync(int loanId)
        => await _context.Loans
            .Include(l => l.User)
            .Include(l => l.ReviewedByUser)
            .FirstOrDefaultAsync(l => l.LoanId == loanId);

    public async Task<List<Loan>> GetByUserAsync(int userId)
        => await _context.Loans
            .Include(l => l.ReviewedByUser)
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

    public async Task<List<Loan>> GetAllPendingAsync()
        => await _context.Loans
            .Include(l => l.User)
            .Where(l => l.Status == "Pending")
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();

    public async Task<List<Loan>> GetAllAsync()
        => await _context.Loans
            .Include(l => l.User)
            .Include(l => l.ReviewedByUser)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

    public async Task UpdateLoanAsync(Loan loan)
    {
        loan.UpdatedAt = DateTime.UtcNow;
        _context.Loans.Update(loan);
        await _context.SaveChangesAsync();
    }
}
