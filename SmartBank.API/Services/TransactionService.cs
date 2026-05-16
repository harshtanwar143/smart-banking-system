using SmartBank.API.Services.Interfaces;
using SmartBank.Data.Repositories;
using SmartBank.Models.DTOs.Transactions;
using SmartBank.Models.Entities;

namespace SmartBank.API.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repo;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(ITransactionRepository repo, ILogger<TransactionService> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    // ─── Deposit ──────────────────────────────────────────────────────────────
    public async Task<TransactionResultDto> DepositAsync(int userId, DepositRequestDto request)
    {
        if (request.Amount <= 0)
            return Fail("Amount must be greater than zero.");

        var account = await _repo.GetAccountAsync(request.AccountId);
        if (account is null)
            return Fail("Account not found.");

        if (account.UserId != userId)
            return Fail("You can only deposit into your own accounts.");

        if (account.Status != "Active")
            return Fail($"Account is {account.Status?.ToLower() ?? "inactive"}.");

        // Update balance
        account.Balance += request.Amount;
        await _repo.UpdateAccountAsync(account);

        var txn = new Transaction
        {
            AccountId         = account.AccountId,
            PerformedByUserId = userId,
            TransactionType   = "Deposit",
            Amount            = request.Amount,
            BalanceAfter      = account.Balance,
            Description       = request.Description ?? "Cash deposit",
            ReferenceNumber   = $"DEP{Guid.NewGuid():N}".Substring(0, 24),
            Channel           = "Online",
            Status            = "Success",
            CreatedAt         = DateTime.UtcNow
        };

        await _repo.AddTransactionAsync(txn);
        _logger.LogInformation("Deposit of {Amount} to account {AccountId} by user {UserId}",
            request.Amount, account.AccountId, userId);

        return new TransactionResultDto
        {
            Success     = true,
            Message     = $"Deposit of INR {request.Amount:N2} successful.",
            NewBalance  = account.Balance,
            Transaction = MapTxn(txn, account.AccountNumber)
        };
    }

    // ─── Withdraw ─────────────────────────────────────────────────────────────
    public async Task<TransactionResultDto> WithdrawAsync(int userId, WithdrawRequestDto request)
    {
        if (request.Amount <= 0)
            return Fail("Amount must be greater than zero.");

        var account = await _repo.GetAccountAsync(request.AccountId);
        if (account is null)
            return Fail("Account not found.");

        if (account.UserId != userId)
            return Fail("You can only withdraw from your own accounts.");

        if (account.Status != "Active")
            return Fail($"Account is {account.Status?.ToLower() ?? "inactive"}.");

        var minBal = account.MinimumBalance ?? 0;
        if (account.Balance - request.Amount < minBal)
            return Fail($"Insufficient balance. Minimum balance of INR {minBal:N2} must be maintained.");

        account.Balance -= request.Amount;
        await _repo.UpdateAccountAsync(account);

        var txn = new Transaction
        {
            AccountId         = account.AccountId,
            PerformedByUserId = userId,
            TransactionType   = "Withdrawal",
            Amount            = request.Amount,
            BalanceAfter      = account.Balance,
            Description       = request.Description ?? "Cash withdrawal",
            ReferenceNumber   = $"WDR{Guid.NewGuid():N}".Substring(0, 24),
            Channel           = "Online",
            Status            = "Success",
            CreatedAt         = DateTime.UtcNow
        };

        await _repo.AddTransactionAsync(txn);
        _logger.LogInformation("Withdrawal of {Amount} from account {AccountId} by user {UserId}",
            request.Amount, account.AccountId, userId);

        return new TransactionResultDto
        {
            Success     = true,
            Message     = $"Withdrawal of INR {request.Amount:N2} successful.",
            NewBalance  = account.Balance,
            Transaction = MapTxn(txn, account.AccountNumber)
        };
    }

    // ─── Transfer (Atomic) ────────────────────────────────────────────────────
    public async Task<TransferResultDto> TransferAsync(int userId, TransferRequestDto request)
    {
        if (request.Amount <= 0)
            return new TransferResultDto { Success = false, Message = "Amount must be greater than zero." };

        var fromAccount = await _repo.GetAccountAsync(request.FromAccountId);
        if (fromAccount is null)
            return new TransferResultDto { Success = false, Message = "Source account not found." };

        if (fromAccount.UserId != userId)
            return new TransferResultDto { Success = false, Message = "You can only transfer from your own account." };

        if (fromAccount.Status != "Active")
            return new TransferResultDto { Success = false, Message = $"Source account is {fromAccount.Status?.ToLower()}." };

        var toAccount = await _repo.GetAccountByNumberAsync(request.ToAccountNumber.Trim());
        if (toAccount is null)
            return new TransferResultDto { Success = false, Message = "Destination account number not found." };

        if (toAccount.AccountId == fromAccount.AccountId)
            return new TransferResultDto { Success = false, Message = "Cannot transfer to the same account." };

        if (toAccount.Status != "Active")
            return new TransferResultDto { Success = false, Message = "Destination account is inactive." };

        var minBal = fromAccount.MinimumBalance ?? 0;
        if (fromAccount.Balance - request.Amount < minBal)
            return new TransferResultDto { Success = false, Message = $"Insufficient balance. Minimum INR {minBal:N2} must be maintained." };

        // Atomic database transaction
        await using var dbTxn = await _repo.BeginTransactionAsync();
        try
        {
            var refNumber = $"TRF{Guid.NewGuid():N}".Substring(0, 24);

            // Create transfer header
            var transfer = new Transfer
            {
                FromAccountId     = fromAccount.AccountId,
                ToAccountId       = toAccount.AccountId,
                Amount            = request.Amount,
                Remarks           = request.Remarks,
                ReferenceNumber   = refNumber,
                Status            = "Pending",
                InitiatedByUserId = userId,
                CreatedAt         = DateTime.UtcNow
            };
            await _repo.AddTransferAsync(transfer);

            // Debit source
            fromAccount.Balance -= request.Amount;
            await _repo.UpdateAccountAsync(fromAccount);

            await _repo.AddTransactionAsync(new Transaction
            {
                AccountId         = fromAccount.AccountId,
                TransferId        = transfer.TransferId,
                PerformedByUserId = userId,
                TransactionType   = "TransferDebit",
                Amount            = request.Amount,
                BalanceAfter      = fromAccount.Balance,
                Description       = $"Transfer to {toAccount.AccountNumber}" + (string.IsNullOrEmpty(request.Remarks) ? "" : $" - {request.Remarks}"),
                ReferenceNumber   = $"DBT-{refNumber}",
                Channel           = "Online",
                Status            = "Success",
                CreatedAt         = DateTime.UtcNow
            });

            // Credit destination
            toAccount.Balance += request.Amount;
            await _repo.UpdateAccountAsync(toAccount);

            await _repo.AddTransactionAsync(new Transaction
            {
                AccountId         = toAccount.AccountId,
                TransferId        = transfer.TransferId,
                PerformedByUserId = userId,
                TransactionType   = "TransferCredit",
                Amount            = request.Amount,
                BalanceAfter      = toAccount.Balance,
                Description       = $"Transfer from {fromAccount.AccountNumber}" + (string.IsNullOrEmpty(request.Remarks) ? "" : $" - {request.Remarks}"),
                ReferenceNumber   = $"CRD-{refNumber}",
                Channel           = "Online",
                Status            = "Success",
                CreatedAt         = DateTime.UtcNow
            });

            // Mark transfer completed
            transfer.Status      = "Completed";
            transfer.CompletedAt = DateTime.UtcNow;
            await _repo.UpdateTransferAsync(transfer);

            await dbTxn.CommitAsync();

            _logger.LogInformation("Transfer {Ref} of {Amount} from {From} to {To} by user {UserId}",
                refNumber, request.Amount, fromAccount.AccountNumber, toAccount.AccountNumber, userId);

            return new TransferResultDto
            {
                Success           = true,
                Message           = $"Transfer of INR {request.Amount:N2} completed successfully.",
                TransferId        = transfer.TransferId,
                ReferenceNumber   = refNumber,
                Amount            = request.Amount,
                NewBalance        = fromAccount.Balance,
                FromAccountNumber = fromAccount.AccountNumber,
                ToAccountNumber   = toAccount.AccountNumber,
                CompletedAt       = transfer.CompletedAt.Value
            };
        }
        catch (Exception ex)
        {
            await dbTxn.RollbackAsync();
            _logger.LogError(ex, "Transfer failed for user {UserId}", userId);
            return new TransferResultDto { Success = false, Message = "Transfer failed. Please try again." };
        }
    }

    // ─── History ──────────────────────────────────────────────────────────────
    public async Task<TransactionHistoryDto> GetHistoryAsync(int userId, int? accountId, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        int skip = (page - 1) * pageSize;
        List<Transaction> transactions;
        int total;

        if (accountId.HasValue)
        {
            // Verify ownership
            if (!await _repo.AccountBelongsToUserAsync(accountId.Value, userId))
                return new TransactionHistoryDto { Page = page, PageSize = pageSize };

            transactions = await _repo.GetHistoryAsync(accountId.Value, skip, pageSize);
            total        = await _repo.GetHistoryCountAsync(accountId.Value);
        }
        else
        {
            transactions = await _repo.GetUserHistoryAsync(userId, skip, pageSize);
            total        = await _repo.GetUserHistoryCountAsync(userId);
        }

        return new TransactionHistoryDto
        {
            Transactions = transactions.Select(t => MapTxn(t, t.Account.AccountNumber)).ToList(),
            TotalCount   = total,
            Page         = page,
            PageSize     = pageSize,
            TotalPages   = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    private static TransactionResultDto Fail(string message) =>
        new() { Success = false, Message = message };

    private static TransactionResponseDto MapTxn(Transaction t, string? accountNumber) => new()
    {
        TransactionId   = t.TransactionId,
        AccountId       = t.AccountId,
        AccountNumber   = accountNumber,
        TransactionType = t.TransactionType ?? "Unknown",
        Amount          = t.Amount,
        BalanceAfter    = t.BalanceAfter,
        Description     = t.Description,
        ReferenceNumber = t.ReferenceNumber,
        Status          = t.Status,
        CreatedAt       = t.CreatedAt ?? DateTime.UtcNow
    };
}
