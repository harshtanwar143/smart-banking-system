using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBank.API.Services;
using SmartBank.Models.DTOs.Support;
using System.Security.Claims;

namespace SmartBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class TicketsController : ControllerBase
{
    private readonly ISupportService _service;

    public TicketsController(ISupportService service)
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

    private bool IsAdmin()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        return role is "Admin" or "Manager" or "Teller";
    }

    /// <summary>Create a new support ticket.</summary>
    [HttpPost("create")]
    [ProducesResponseType(typeof(TicketResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(TicketResultDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTicketDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var result = await _service.CreateTicketAsync(userId, request);
        return result.Success
            ? CreatedAtAction(nameof(MyTickets), result)
            : BadRequest(result);
    }

    /// <summary>Get all tickets for the current user.</summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(TicketListDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> MyTickets()
    {
        var userId = GetCurrentUserId();
        var result = await _service.GetMyTicketsAsync(userId);
        return Ok(result);
    }

    /// <summary>Get a specific ticket by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TicketResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var userId  = GetCurrentUserId();
        var result  = await _service.GetTicketByIdAsync(id, userId, IsAdmin());
        return result is null ? NotFound(new { Message = "Ticket not found." }) : Ok(result);
    }
}
