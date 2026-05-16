namespace SmartBank.Models.DTOs.Accounts;

public class CreateAccountDto
{
    public string AccountType { get; set; } = null!; // "Savings" or "Current"
    public decimal InitialDeposit { get; set; }
}

public class AccountResponseDto
{
    public int AccountId { get; set; }
    public string AccountNumber { get; set; } = null!;
    public string AccountType { get; set; } = null!;
    public decimal Balance { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
}

public class AccountListResponseDto
{
    public string Message { get; set; } = null!;
    public List<AccountResponseDto> Data { get; set; } = new();
    public int TotalAccounts { get; set; }
}

public class CreateAccountResponseDto
{
    public string Message { get; set; } = null!;
    public AccountResponseDto Data { get; set; } = null!;
}
