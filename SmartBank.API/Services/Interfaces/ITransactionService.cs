using SmartBank.Models.DTOs.Transactions;

namespace SmartBank.API.Services.Interfaces;

public interface ITransactionService
{
    Task<TransactionResultDto> DepositAsync(int userId, DepositRequestDto request);
    Task<TransactionResultDto> WithdrawAsync(int userId, WithdrawRequestDto request);
    Task<TransferResultDto>    TransferAsync(int userId, TransferRequestDto request);
    Task<TransactionHistoryDto> GetHistoryAsync(int userId, int? accountId, int page, int pageSize);
}
