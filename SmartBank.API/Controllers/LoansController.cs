using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBank.API.Services;
using SmartBank.Models.DTOs.Loans;
using System.Security.Claims;

namespace SmartBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class LoansController : ControllerBase
{
    private readonly ILoanService _service;

    public LoansController(ILoanService service)
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

    /// <summary>Apply for a new loan.</summary>
    [HttpPost("apply")]
    [ProducesResponseType(typeof(LoanApplyResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(LoanApplyResultDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Apply([FromBody] LoanApplicationDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var result = await _service.ApplyAsync(userId, request);
        return result.Success
            ? CreatedAtAction(nameof(MyLoans), result)
            : BadRequest(result);
    }

    /// <summary>Get all loans for the current user.</summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(LoanListDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> MyLoans()
    {
        var userId = GetCurrentUserId();
        var result = await _service.GetMyLoansAsync(userId);
        return Ok(result);
    }

    /// <summary>Alias for MyLoans (matches SRS spec).</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(LoanListDto), StatusCodes.Status200OK)]
    public Task<IActionResult> Status() => MyLoans();
}
