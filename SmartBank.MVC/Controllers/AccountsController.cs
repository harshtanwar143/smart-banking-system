using Microsoft.AspNetCore.Mvc;
using SmartBank.Models.DTOs.Accounts;

namespace SmartBank.MVC.Controllers;

public class AccountsController : SecureControllerBase
{
    public AccountsController(IHttpClientFactory http, ILogger<AccountsController> logger)
        : base(http, logger) { }

    // GET /Accounts
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var list = await ApiGetAsync<AccountListResponseDto>("api/accounts");
        return View(list ?? new AccountListResponseDto { Message = "No accounts" });
    }

    // GET /Accounts/Open
    [HttpGet]
    public IActionResult Open() => View(new CreateAccountDto { AccountType = "Savings", InitialDeposit = 500 });

    // POST /Accounts/Open
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(CreateAccountDto model)
    {
        if (!ModelState.IsValid) return View(model);

        var (ok, result, error) = await ApiPostAsync<CreateAccountResponseDto>("api/accounts/create", model);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "Could not open account.");
            return View(model);
        }

        TempData["SuccessMessage"] =
            $"Account {result?.Data.AccountNumber} opened successfully with balance INR {result?.Data.Balance:N2}.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Accounts/Details/{id}
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var list = await ApiGetAsync<AccountListResponseDto>("api/accounts");
        if (list?.Data is null)
        {
            TempData["ErrorMessage"] = "Could not load account.";
            return RedirectToAction(nameof(Index));
        }

        var account = list.Data.FirstOrDefault(a => a.AccountId == id);
        if (account is null)
        {
            TempData["ErrorMessage"] = "Account not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(account);
    }
}
