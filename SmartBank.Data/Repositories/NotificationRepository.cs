using Microsoft.EntityFrameworkCore;
using SmartBank.Data.Context;
using SmartBank.Models.Entities;

namespace SmartBank.Data.Repositories;

public interface INotificationRepository
{
    Task<Notification> AddAsync(Notification notification);
    Task<List<Notification>> GetByUserAsync(int userId, int skip, int take);
    Task<int> GetUnreadCountAsync(int userId);
    Task<int> GetTotalCountAsync(int userId);
    Task<Notification?> GetByIdAsync(int notificationId);
    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync(int userId);
}

public class NotificationRepository : INotificationRepository
{
    private readonly SmartOnlineBankingDbContext _context;

    public NotificationRepository(SmartOnlineBankingDbContext context)
    {
        _context = context;
    }

    public async Task<Notification> AddAsync(Notification notification)
    {
        notification.CreatedAt ??= DateTime.UtcNow;
        notification.IsRead    ??= false;
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task<List<Notification>> GetByUserAsync(int userId, int skip, int take)
        => await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip).Take(take)
            .ToListAsync();

    public async Task<int> GetUnreadCountAsync(int userId)
        => await _context.Notifications.CountAsync(n => n.UserId == userId && n.IsRead == false);

    public async Task<int> GetTotalCountAsync(int userId)
        => await _context.Notifications.CountAsync(n => n.UserId == userId);

    public async Task<Notification?> GetByIdAsync(int notificationId)
        => await _context.Notifications.FirstOrDefaultAsync(n => n.NotificationId == notificationId);

    public async Task MarkAsReadAsync(int notificationId)
    {
        var n = await _context.Notifications.FirstOrDefaultAsync(x => x.NotificationId == notificationId);
        if (n is null) return;
        n.IsRead = true;
        n.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var notifs = await _context.Notifications
            .Where(n => n.UserId == userId && n.IsRead == false)
            .ToListAsync();

        foreach (var n in notifs)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
    }
}
