using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBank.API.Services.Interfaces;
using SmartBank.Models.DTOs.Transactions;
using System.Security.Claims;

namespace SmartBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _service;

    public TransactionsController(ITransactionService service)
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

    /// <summary>Deposit funds into an account.</summary>
    [HttpPost("deposit")]
    [ProducesResponseType(typeof(TransactionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(TransactionResultDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Deposit([FromBody] DepositRequestDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _service.DepositAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Withdraw funds from an account.</summary>
    [HttpPost("withdraw")]
    [ProducesResponseType(typeof(TransactionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(TransactionResultDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawRequestDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _service.WithdrawAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Transfer funds between accounts (atomic).</summary>
    [HttpPost("transfer")]
    [ProducesResponseType(typeof(TransferResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(TransferResultDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Transfer([FromBody] TransferRequestDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _service.TransferAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Get transaction history. Optionally filter by accountId.</summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(TransactionHistoryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> History(
        [FromQuery] int? accountId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        var result = await _service.GetHistoryAsync(userId, accountId, page, pageSize);
        return Ok(result);
    }
}
