using Microsoft.EntityFrameworkCore;
using SmartBank.Data.Context;
using SmartBank.Models.Entities;

namespace SmartBank.Data.Repositories;

public interface ISupportRepository
{
    Task<SupportTicket> AddAsync(SupportTicket ticket);
    Task<SupportTicket?> GetByIdAsync(int ticketId);
    Task<List<SupportTicket>> GetByUserAsync(int userId);
    Task<List<SupportTicket>> GetAllAsync();
    Task<List<SupportTicket>> GetOpenAsync();
    Task UpdateAsync(SupportTicket ticket);
}

public class SupportRepository : ISupportRepository
{
    private readonly SmartOnlineBankingDbContext _context;

    public SupportRepository(SmartOnlineBankingDbContext context)
    {
        _context = context;
    }

    public async Task<SupportTicket> AddAsync(SupportTicket ticket)
    {
        ticket.CreatedAt ??= DateTime.UtcNow;
        ticket.Status    ??= "Open";
        _context.SupportTickets.Add(ticket);
        await _context.SaveChangesAsync();
        return ticket;
    }

    public async Task<SupportTicket?> GetByIdAsync(int ticketId)
        => await _context.SupportTickets
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

    public async Task<List<SupportTicket>> GetByUserAsync(int userId)
        => await _context.SupportTickets
            .Where(t => t.CreatedByUserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public async Task<List<SupportTicket>> GetAllAsync()
        => await _context.SupportTickets
            .Include(t => t.CreatedByUser)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public async Task<List<SupportTicket>> GetOpenAsync()
        => await _context.SupportTickets
            .Include(t => t.CreatedByUser)
            .Where(t => t.Status != "Resolved" && t.Status != "Closed")
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public async Task UpdateAsync(SupportTicket ticket)
    {
        ticket.UpdatedAt = DateTime.UtcNow;
        _context.SupportTickets.Update(ticket);
        await _context.SaveChangesAsync();
    }
}
