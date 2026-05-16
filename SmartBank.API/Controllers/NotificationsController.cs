using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBank.API.Services;
using SmartBank.Models.DTOs.Notifications;
using System.Security.Claims;

namespace SmartBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service)
    {
        _service = service;
    }

    private int GetCurrentUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(raw) || !int.TryParse(raw, out var id))
            throw new UnauthorizedAccessException("Invalid token.");
        return id;
    }

    /// <summary>Get notifications for the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(NotificationListDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        var result = await _service.GetForUserAsync(userId, page, pageSize);
        return Ok(result);
    }

    /// <summary>Get the unread notification count.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UnreadCount()
    {
        var userId = GetCurrentUserId();
        var count  = await _service.GetUnreadCountAsync(userId);
        return Ok(new { UnreadCount = count });
    }

    /// <summary>Mark a notification as read.</summary>
    [HttpPost("{id:int}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCurrentUserId();
        var ok     = await _service.MarkAsReadAsync(userId, id);
        return ok ? Ok(new { Success = true }) : NotFound();
    }

    /// <summary>Mark all notifications as read.</summary>
    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        await _service.MarkAllAsReadAsync(userId);
        return Ok(new { Success = true });
    }
}
