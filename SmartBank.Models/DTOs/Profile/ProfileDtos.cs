namespace SmartBank.Models.DTOs.Profile;

public class ProfileDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? DateOfBirth { get; set; }
}

public class UpdateProfileDto
{
    public string Name { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Address { get; set; } = null!;
}

public class ProfileResponseDto
{
    public string Message { get; set; } = null!;
    public ProfileDto Data { get; set; } = null!;
}