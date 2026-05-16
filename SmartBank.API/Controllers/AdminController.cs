using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBank.API.Services;
using SmartBank.Models.DTOs.Admin;
using SmartBank.Models.DTOs.Loans;
using SmartBank.Models.DTOs.Support;
using System.Security.Claims;

namespace SmartBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ILoanService _loanService;
    private readonly ISupportService _supportService;

    public AdminController(IAdminService adminService, ILoanService loanService, ISupportService supportService)
    {
        _adminService    = adminService;
        _loanService     = loanService;
        _supportService  = supportService;
    }

    private int GetCurrentUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(raw) || !int.TryParse(raw, out var id))
            throw new UnauthorizedAccessException("Invalid token.");
        return id;
    }

    /// <summary>Get all users in the system.</summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(AdminUserListDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Users()
    {
        var result = await _adminService.GetAllUsersAsync();
        return Ok(result);
    }

    /// <summary>Freeze or unfreeze a user account.</summary>
    [HttpPost("freeze")]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminActionResultDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Freeze([FromBody] FreezeUserDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var adminId = GetCurrentUserId();
        var result  = await _adminService.FreezeUserAsync(adminId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Get all pending loans for review.</summary>
    [HttpGet("loans/pending")]
    [ProducesResponseType(typeof(LoanListDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PendingLoans()
        => Ok(await _loanService.GetPendingLoansAsync());

    /// <summary>Get all loans (any status).</summary>
    [HttpGet("loans")]
    [ProducesResponseType(typeof(LoanListDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AllLoans()
        => Ok(await _loanService.GetAllLoansAsync());

    /// <summary>Approve or reject a loan application.</summary>
    [HttpPost("loan/approve")]
    [ProducesResponseType(typeof(LoanApplyResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoanApplyResultDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReviewLoan([FromBody] LoanReviewDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var adminId = GetCurrentUserId();
        var result  = await _loanService.ReviewAsync(adminId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Get all support tickets.</summary>
    [HttpGet("tickets")]
    [ProducesResponseType(typeof(TicketListDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AllTickets()
        => Ok(await _supportService.GetAllTicketsAsync());

    /// <summary>Resolve a support ticket.</summary>
    [HttpPost("ticket/resolve")]
    [ProducesResponseType(typeof(TicketResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(TicketResultDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResolveTicket([FromBody] ResolveTicketDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var adminId = GetCurrentUserId();
        var result  = await _supportService.ResolveTicketAsync(adminId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Get admin dashboard stats.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Dashboard()
        => Ok(await _adminService.GetDashboardStatsAsync());

    /// <summary>Get full reports including low-balance accounts and daily summary.</summary>
    [HttpGet("reports")]
    [ProducesResponseType(typeof(ReportsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reports()
        => Ok(await _adminService.GetReportsAsync());
}
