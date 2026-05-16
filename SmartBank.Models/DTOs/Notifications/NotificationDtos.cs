namespace SmartBank.Models.DTOs.Notifications;

public class NotificationDto
{
    public int NotificationId { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotificationListDto
{
    public List<NotificationDto> Notifications { get; set; } = new();
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
}
