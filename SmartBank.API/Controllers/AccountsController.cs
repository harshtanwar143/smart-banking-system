using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBank.Data.Context;
using SmartBank.Models.DTOs.Accounts;
using SmartBank.Models.Entities;
using System.Security.Claims;

namespace SmartBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class AccountsController : ControllerBase
{
    private readonly SmartOnlineBankingDbContext _context;

    public AccountsController(SmartOnlineBankingDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }
        return userId;
    }

    private string GenerateAccountNumber(string accountType)
    {
        var prefix = accountType.ToLower() == "savings" ? "SB" : "CA";
        var random = new Random();
        var digits = "";
        for (int i = 0; i < 10; i++)
        {
            digits += random.Next(0, 10);
        }
        return $"{prefix}{digits}";
    }

    /// <summary>Create a new bank account.</summary>
    [HttpPost("create")]
    [ProducesResponseType(typeof(CreateAccountResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto request)
    {
        try
        {
            // Validations
            if (string.IsNullOrWhiteSpace(request.AccountType))
                return BadRequest(new { Message = "Account type is required" });

            var accountType = request.AccountType.Trim();
            if (!accountType.Equals("Savings", StringComparison.OrdinalIgnoreCase) && 
                !accountType.Equals("Current", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { Message = "Account type must be 'Savings' or 'Current'" });
            }

            if (request.InitialDeposit < 500)
                return BadRequest(new { Message = "Minimum deposit is ₹500" });

            var userId = GetCurrentUserId();

            // Generate unique account number
            string accountNumber;
            bool exists;
            do
            {
                accountNumber = GenerateAccountNumber(accountType);
                exists = await _context.Accounts.AnyAsync(a => a.AccountNumber == accountNumber);
            } while (exists);

            var account = new Account
            {
                UserId = userId,
                AccountNumber = accountNumber,
                AccountType = accountType,
                Balance = request.InitialDeposit,
                Status = "Active",
                Currency = "INR",
                OpenedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            var response = new CreateAccountResponseDto
            {
                Message = "Account created successfully",
                Data = new AccountResponseDto
                {
                    AccountId = account.AccountId,
                    AccountNumber = account.AccountNumber,
                    AccountType = account.AccountType,
                    Balance = account.Balance,
                    Status = account.Status,
                    CreatedDate = account.OpenedAt.Value
                }
            };

            return CreatedAtAction(nameof(CreateAccount), response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    /// <summary>List all customer accounts.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(AccountListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAccounts()
    {
        try
        {
            var userId = GetCurrentUserId();

            var accounts = await _context.Accounts
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.OpenedAt)
                .Select(a => new AccountResponseDto
                {
                    AccountId = a.AccountId,
                    AccountNumber = a.AccountNumber,
                    AccountType = a.AccountType,
                    Balance = a.Balance,
                    Status = a.Status,
                    CreatedDate = a.OpenedAt ?? DateTime.MinValue
                })
                .ToListAsync();

            var response = new AccountListResponseDto
            {
                Message = "Accounts retrieved successfully",
                Data = accounts,
                TotalAccounts = accounts.Count
            };

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }
}
