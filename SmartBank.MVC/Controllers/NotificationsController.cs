using Microsoft.AspNetCore.Mvc;
using SmartBank.Models.DTOs.Notifications;

namespace SmartBank.MVC.Controllers;

public class NotificationsController : SecureControllerBase
{
    public NotificationsController(IHttpClientFactory http, ILogger<NotificationsController> logger)
        : base(http, logger) { }

    // GET /Notifications
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var list = await ApiGetAsync<NotificationListDto>("api/notifications?page=1&pageSize=50");
        return View(list ?? new NotificationListDto());
    }

    // POST /Notifications/MarkRead/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var (ok, _, _) = await ApiPostAsync<object>($"api/notifications/{id}/read", new { });
        if (!ok) TempData["ErrorMessage"] = "Could not mark notification as read.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Notifications/MarkAllRead
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var (ok, _, _) = await ApiPostAsync<object>("api/notifications/read-all", new { });
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "All notifications marked as read." : "Could not update notifications.";
        return RedirectToAction(nameof(Index));
    }
}
