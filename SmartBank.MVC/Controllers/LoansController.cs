using Microsoft.AspNetCore.Mvc;
using SmartBank.Models.DTOs.Loans;

namespace SmartBank.MVC.Controllers;

public class LoansController : SecureControllerBase
{
    public LoansController(IHttpClientFactory http, ILogger<LoansController> logger)
        : base(http, logger) { }

    // GET /Loans
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var loans = await ApiGetAsync<LoanListDto>("api/loans/my");
        return View(loans ?? new LoanListDto());
    }

    // GET /Loans/Apply
    [HttpGet]
    public IActionResult Apply()
        => View(new LoanApplicationDto { LoanType = "Personal", TenureMonths = 12 });

    // POST /Loans/Apply
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(LoanApplicationDto model)
    {
        if (!ModelState.IsValid) return View(model);

        var (ok, result, error) = await ApiPostAsync<LoanApplyResultDto>("api/loans/apply", model);
        if (!ok || result is null || !result.Success)
        {
            ModelState.AddModelError(string.Empty, error ?? result?.Message ?? "Loan application failed.");
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // GET /Loans/Calculator (helper)
    [HttpGet]
    public IActionResult Calculator() => View();
}
