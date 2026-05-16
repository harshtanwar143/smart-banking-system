using SmartBank.Data.Repositories;
using SmartBank.Models.DTOs.Notifications;

namespace SmartBank.API.Services;

public interface INotificationService
{
    Task<NotificationListDto> GetForUserAsync(int userId, int page, int pageSize);
    Task<int> GetUnreadCountAsync(int userId);
    Task<bool> MarkAsReadAsync(int userId, int notificationId);
    Task MarkAllAsReadAsync(int userId);
}

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;

    public NotificationService(INotificationRepository repo)
    {
        _repo = repo;
    }

    public async Task<NotificationListDto> GetForUserAsync(int userId, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var skip   = (page - 1) * pageSize;
        var items  = await _repo.GetByUserAsync(userId, skip, pageSize);
        var total  = await _repo.GetTotalCountAsync(userId);
        var unread = await _repo.GetUnreadCountAsync(userId);

        return new NotificationListDto
        {
            Notifications = items.Select(n => new NotificationDto
            {
                NotificationId = n.NotificationId,
                Title          = n.Title,
                Message        = n.Message,
                Type           = n.Type,
                IsRead         = n.IsRead ?? false,
                CreatedAt      = n.CreatedAt ?? DateTime.UtcNow
            }).ToList(),
            TotalCount  = total,
            UnreadCount = unread
        };
    }

    public Task<int> GetUnreadCountAsync(int userId) => _repo.GetUnreadCountAsync(userId);

    public async Task<bool> MarkAsReadAsync(int userId, int notificationId)
    {
        var n = await _repo.GetByIdAsync(notificationId);
        if (n is null || n.UserId != userId) return false;
        await _repo.MarkAsReadAsync(notificationId);
        return true;
    }

    public Task MarkAllAsReadAsync(int userId) => _repo.MarkAllAsReadAsync(userId);
}
