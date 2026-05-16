using System.ComponentModel.DataAnnotations;

namespace SmartBank.Models.DTOs.Support;

public class CreateTicketDto
{
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = null!;

    [Required]
    [MinLength(10, ErrorMessage = "Description must be at least 10 characters.")]
    [MaxLength(2000)]
    public string Description { get; set; } = null!;

    [MaxLength(50)]
    public string? Category { get; set; } // Account, Transaction, Loan, Card, Other

    [MaxLength(20)]
    public string? Priority { get; set; } = "Medium"; // Low, Medium, High
}

public class TicketResponseDto
{
    public int TicketId { get; set; }
    public string Subject { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? Category { get; set; }
    public string? Priority { get; set; }
    public string Status { get; set; } = "Open";
    public string? Resolution { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class TicketResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TicketResponseDto? Ticket { get; set; }
}

public class TicketListDto
{
    public List<TicketResponseDto> Tickets { get; set; } = new();
    public int TotalCount { get; set; }
    public int OpenCount { get; set; }
    public int ResolvedCount { get; set; }
}

public class ResolveTicketDto
{
    [Required]
    public int TicketId { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Resolution { get; set; } = null!;
}
