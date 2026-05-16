using Microsoft.AspNetCore.Mvc;
using SmartBank.Models.DTOs.Admin;
using SmartBank.Models.DTOs.Loans;
using SmartBank.Models.DTOs.Support;

namespace SmartBank.MVC.Controllers;

public class AdminController : SecureControllerBase
{
    public AdminController(IHttpClientFactory http, ILogger<AdminController> logger)
        : base(http, logger) { }

    // Role gate
    private IActionResult? CheckAdminAccess()
    {
        var role = CurrentUserRole();
        if (role is not ("Admin" or "Manager"))
        {
            TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
            return RedirectToAction("Index", "Dashboard");
        }
        return null;
    }

    // GET /Admin
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var deny = CheckAdminAccess(); if (deny is not null) return deny;

        var stats = await ApiGetAsync<DashboardStatsDto>("api/admin/dashboard");
        return View(stats ?? new DashboardStatsDto());
    }

    // GET /Admin/Users
    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var deny = CheckAdminAccess(); if (deny is not null) return deny;

        var list = await ApiGetAsync<AdminUserListDto>("api/admin/users");
        return View(list ?? new AdminUserListDto());
    }

    // POST /Admin/FreezeUser
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeUser(int userId, bool freeze, string? reason)
    {
        var deny = CheckAdminAccess(); if (deny is not null) return deny;

        var dto = new FreezeUserDto { UserId = userId, Freeze = freeze, Reason = reason };
        var (ok, result, error) = await ApiPostAsync<AdminActionResultDto>("api/admin/freeze", dto);
        if (ok && result?.Success == true)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = error ?? result?.Message ?? "Action failed.";

        return RedirectToAction(nameof(Users));
    }

    // GET /Admin/Loans
    [HttpGet]
    public async Task<IActionResult> Loans()
    {
        var deny = CheckAdminAccess(); if (deny is not null) return deny;

        var list = await ApiGetAsync<LoanListDto>("api/admin/loans");
        return View(list ?? new LoanListDto());
    }

    // POST /Admin/ReviewLoan
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewLoan(LoanReviewDto model)
    {
        var deny = CheckAdminAccess(); if (deny is not null) return deny;

        var (ok, result, error) = await ApiPostAsync<LoanApplyResultDto>("api/admin/loan/approve", model);
        if (ok && result?.Success == true)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = error ?? result?.Message ?? "Loan review failed.";

        return RedirectToAction(nameof(Loans));
    }

    // GET /Admin/Tickets
    [HttpGet]
    public async Task<IActionResult> Tickets()
    {
        var deny = CheckAdminAccess(); if (deny is not null) return deny;

        var list = await ApiGetAsync<TicketListDto>("api/admin/tickets");
        return View(list ?? new TicketListDto());
    }

    // POST /Admin/ResolveTicket
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveTicket(int ticketId, string resolution)
    {
        var deny = CheckAdminAccess(); if (deny is not null) return deny;

        var dto = new ResolveTicketDto { TicketId = ticketId, Resolution = resolution };
        var (ok, result, error) = await ApiPostAsync<TicketResultDto>("api/admin/ticket/resolve", dto);
        if (ok && result?.Success == true)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = error ?? result?.Message ?? "Could not resolve ticket.";

        return RedirectToAction(nameof(Tickets));
    }

    // GET /Admin/Reports
    [HttpGet]
    public async Task<IActionResult> Reports()
    {
        var deny = CheckAdminAccess(); if (deny is not null) return deny;

        var reports = await ApiGetAsync<ReportsDto>("api/admin/reports");
        return View(reports ?? new ReportsDto());
    }
}
