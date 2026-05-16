using Microsoft.AspNetCore.Mvc;
using SmartBank.Models.DTOs.Accounts;
using SmartBank.Models.DTOs.Transactions;

namespace SmartBank.MVC.Controllers;

public class TransactionsController : SecureControllerBase
{
    public TransactionsController(IHttpClientFactory http, ILogger<TransactionsController> logger)
        : base(http, logger) { }

    // ─── Helper: load user accounts for dropdown ──────────────────────────────
    private async Task<List<AccountResponseDto>> LoadAccountsAsync()
    {
        var list = await ApiGetAsync<AccountListResponseDto>("api/accounts");
        return list?.Data ?? new List<AccountResponseDto>();
    }

    // ─── Deposit ──────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Deposit()
    {
        ViewBag.Accounts = await LoadAccountsAsync();
        return View(new DepositRequestDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deposit(DepositRequestDto model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Accounts = await LoadAccountsAsync();
            return View(model);
        }

        var (ok, result, error) = await ApiPostAsync<TransactionResultDto>("api/transactions/deposit", model);
        if (!ok || result is null || !result.Success)
        {
            ModelState.AddModelError(string.Empty, error ?? result?.Message ?? "Deposit failed.");
            ViewBag.Accounts = await LoadAccountsAsync();
            return View(model);
        }

        TempData["SuccessMessage"] =
            $"{result.Message} New balance: INR {result.NewBalance:N2}.";
        return RedirectToAction(nameof(History), new { accountId = model.AccountId });
    }

    // ─── Withdraw ─────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Withdraw()
    {
        ViewBag.Accounts = await LoadAccountsAsync();
        return View(new WithdrawRequestDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(WithdrawRequestDto model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Accounts = await LoadAccountsAsync();
            return View(model);
        }

        var (ok, result, error) = await ApiPostAsync<TransactionResultDto>("api/transactions/withdraw", model);
        if (!ok || result is null || !result.Success)
        {
            ModelState.AddModelError(string.Empty, error ?? result?.Message ?? "Withdrawal failed.");
            ViewBag.Accounts = await LoadAccountsAsync();
            return View(model);
        }

        TempData["SuccessMessage"] =
            $"{result.Message} New balance: INR {result.NewBalance:N2}.";
        return RedirectToAction(nameof(History), new { accountId = model.AccountId });
    }

    // ─── Transfer ─────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Transfer()
    {
        ViewBag.Accounts = await LoadAccountsAsync();
        return View(new TransferRequestDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Transfer(TransferRequestDto model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Accounts = await LoadAccountsAsync();
            return View(model);
        }

        var (ok, result, error) = await ApiPostAsync<TransferResultDto>("api/transactions/transfer", model);
        if (!ok || result is null || !result.Success)
        {
            ModelState.AddModelError(string.Empty, error ?? result?.Message ?? "Transfer failed.");
            ViewBag.Accounts = await LoadAccountsAsync();
            return View(model);
        }

        TempData["SuccessMessage"] =
            $"Transfer of INR {result.Amount:N2} to {result.ToAccountNumber} completed. Ref: {result.ReferenceNumber}";
        return RedirectToAction(nameof(History), new { accountId = model.FromAccountId });
    }

    // ─── History / Statement ──────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> History(int? accountId, int page = 1)
    {
        var url = $"api/transactions/history?page={page}&pageSize=20"
                  + (accountId.HasValue ? $"&accountId={accountId.Value}" : "");

        var history  = await ApiGetAsync<TransactionHistoryDto>(url);
        ViewBag.Accounts        = await LoadAccountsAsync();
        ViewBag.SelectedAccount = accountId;

        return View(history ?? new TransactionHistoryDto { Page = page, PageSize = 20 });
    }
}
