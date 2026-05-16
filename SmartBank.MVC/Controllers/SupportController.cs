using Microsoft.AspNetCore.Mvc;
using SmartBank.Models.DTOs.Support;

namespace SmartBank.MVC.Controllers;

public class SupportController : SecureControllerBase
{
    public SupportController(IHttpClientFactory http, ILogger<SupportController> logger)
        : base(http, logger) { }

    // GET /Support
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var list = await ApiGetAsync<TicketListDto>("api/tickets/my");
        return View(list ?? new TicketListDto());
    }

    // GET /Support/Create
    [HttpGet]
    public IActionResult Create()
        => View(new CreateTicketDto { Category = "Account", Priority = "Medium" });

    // POST /Support/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTicketDto model)
    {
        if (!ModelState.IsValid) return View(model);

        var (ok, result, error) = await ApiPostAsync<TicketResultDto>("api/tickets/create", model);
        if (!ok || result is null || !result.Success)
        {
            ModelState.AddModelError(string.Empty, error ?? result?.Message ?? "Could not create ticket.");
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // GET /Support/Details/{id}
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var ticket = await ApiGetAsync<TicketResponseDto>($"api/tickets/{id}");
        if (ticket is null)
        {
            TempData["ErrorMessage"] = "Ticket not found.";
            return RedirectToAction(nameof(Index));
        }
        return View(ticket);
    }
}
