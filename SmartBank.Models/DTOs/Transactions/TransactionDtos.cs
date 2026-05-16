namespace SmartBank.Models.DTOs.Transactions;

// ─── Request DTOs ────────────────────────────────────────────────────────────

public class DepositRequestDto
{
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class WithdrawRequestDto
{
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class TransferRequestDto
{
    public int FromAccountId { get; set; }

    /// <summary>Destination account number (not ID, for security). Must exist.</summary>
    public string ToAccountNumber { get; set; } = null!;

    public decimal Amount { get; set; }
    public string? Remarks { get; set; }
}

// ─── Response DTOs ───────────────────────────────────────────────────────────

public class TransactionResponseDto
{
    public int TransactionId { get; set; }
    public int AccountId { get; set; }
    public string? AccountNumber { get; set; }
    public string TransactionType { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TransactionResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TransactionResponseDto? Transaction { get; set; }
    public decimal NewBalance { get; set; }
}

public class TransferResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? TransferId { get; set; }
    public string? ReferenceNumber { get; set; }
    public decimal Amount { get; set; }
    public decimal NewBalance { get; set; }
    public string? FromAccountNumber { get; set; }
    public string? ToAccountNumber { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class TransactionHistoryDto
{
    public List<TransactionResponseDto> Transactions { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
