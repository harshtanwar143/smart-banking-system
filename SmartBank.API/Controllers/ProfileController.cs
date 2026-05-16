using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBank.Data.Context;
using SmartBank.Models.DTOs.Profile;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace SmartBank.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ProfileController : ControllerBase
{
    private readonly SmartOnlineBankingDbContext _context;

    public ProfileController(SmartOnlineBankingDbContext context)
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

    /// <summary>Get customer profile.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
                return NotFound(new { Message = "User not found" });

            var profile = new ProfileDto
            {
                UserId = user.UserId,
                Name = $"{user.FirstName} {user.LastName}",
                Email = user.Email,
                Phone = user.PhoneNumber,
                Address = user.Address,
                DateOfBirth = user.DateOfBirth?.ToString("yyyy-MM-dd")
            };

            return Ok(profile);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    /// <summary>Update customer profile.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(ProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
    {
        try
        {
            // Validations
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { Message = "Name is required" });

            if (string.IsNullOrWhiteSpace(request.Phone))
                return BadRequest(new { Message = "Phone is required" });

            if (string.IsNullOrWhiteSpace(request.Address))
                return BadRequest(new { Message = "Address is required" });

            // Phone validation - exactly 10 digits
            if (!Regex.IsMatch(request.Phone, @"^\d{10}$"))
                return BadRequest(new { Message = "Phone must be exactly 10 digits" });

            var userId = GetCurrentUserId();
            
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
                return NotFound(new { Message = "User not found" });

            // Split name into FirstName and LastName
            var nameParts = request.Name.Trim().Split(' ', 2);
            user.FirstName = nameParts[0];
            user.LastName = nameParts.Length > 1 ? nameParts[1] : "";

            // Update fields (Email cannot be changed)
            user.PhoneNumber = request.Phone;
            user.Address = request.Address;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new ProfileResponseDto
            {
                Message = "Profile updated successfully",
                Data = new ProfileDto
                {
                    UserId = user.UserId,
                    Name = $"{user.FirstName} {user.LastName}",
                    Phone = user.PhoneNumber,
                    Address = user.Address
                }
            };

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }
}