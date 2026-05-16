using Microsoft.AspNetCore.Mvc;
using SmartBank.Models.DTOs.Auth;
using System.Net.Http.Json;
using System.Text.Json;

namespace SmartBank.MVC.Controllers;

public class AuthController : Controller
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration     _config;
    private readonly ILogger<AuthController> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthController(IHttpClientFactory httpFactory, IConfiguration config, ILogger<AuthController> logger)
    {
        _httpFactory = httpFactory;
        _config      = config;
        _logger      = logger;
    }

    // ─── GET /Auth/Login ──────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (Request.Cookies.ContainsKey("SmartBankToken"))
            return RedirectToAction("Index", "Dashboard");
        return View();
    }

    // ─── POST /Auth/Login ─────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequestDto model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var client = _httpFactory.CreateClient("SmartBankAPI");

        try
        {
            var response = await client.PostAsJsonAsync("api/auth/login", model);
            var content  = await response.Content.ReadAsStringAsync();
            var result   = JsonSerializer.Deserialize<AuthResponseDto>(content, JsonOpts);

            if (result is null || !result.Success)
            {
                ModelState.AddModelError(string.Empty, result?.Message ?? "Login failed.");
                return View(model);
            }

            SetAuthCookies(result);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            // Admin users land on Admin dashboard
            if (result.User?.RoleName is "Admin" or "Manager")
                return RedirectToAction("Index", "Admin");

            return RedirectToAction("Index", "Dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed against API");
            ModelState.AddModelError(string.Empty, "Cannot reach the API server. Please try again.");
            return View(model);
        }
    }

    // ─── GET /Auth/Register ───────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Register()
    {
        if (Request.Cookies.ContainsKey("SmartBankToken"))
            return RedirectToAction("Index", "Dashboard");
        return View();
    }

    // ─── POST /Auth/Register ──────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequestDto model)
    {
        if (!ModelState.IsValid) return View(model);

        var client = _httpFactory.CreateClient("SmartBankAPI");

        try
        {
            var response = await client.PostAsJsonAsync("api/auth/register", model);
            var content  = await response.Content.ReadAsStringAsync();
            var result   = JsonSerializer.Deserialize<AuthResponseDto>(content, JsonOpts);

            if (result is null || !result.Success)
            {
                ModelState.AddModelError(string.Empty, result?.Message ?? "Registration failed.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Account created successfully. Please sign in.";
            return RedirectToAction(nameof(Login));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed against API");
            ModelState.AddModelError(string.Empty, "Cannot reach the API server. Please try again.");
            return View(model);
        }
    }

    // ─── GET /Auth/Logout ─────────────────────────────────────────────────────
    public IActionResult Logout()
    {
        ClearAuthCookies();
        TempData["SuccessMessage"] = "You have been signed out.";
        return RedirectToAction(nameof(Login));
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    private void SetAuthCookies(AuthResponseDto result)
    {
        var cookieOpts = new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Strict,
            Expires  = DateTimeOffset.UtcNow.AddDays(1)
        };

        Response.Cookies.Append("SmartBankToken", result.Token!, cookieOpts);

        // Non-HttpOnly cookies for UI (no sensitive data)
        var uiCookieOpts = new CookieOptions
        {
            HttpOnly = false,
            Secure   = true,
            SameSite = SameSiteMode.Strict,
            Expires  = DateTimeOffset.UtcNow.AddDays(1)
        };

        if (result.User is not null)
        {
            Response.Cookies.Append("UserName",
                $"{result.User.FirstName} {result.User.LastName}", uiCookieOpts);
            Response.Cookies.Append("UserRole",  result.User.RoleName, uiCookieOpts);
            Response.Cookies.Append("UserId",    result.User.UserId.ToString(), uiCookieOpts);
            Response.Cookies.Append("UserEmail", result.User.Email, uiCookieOpts);
        }
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete("SmartBankToken");
        Response.Cookies.Delete("UserName");
        Response.Cookies.Delete("UserRole");
        Response.Cookies.Delete("UserId");
        Response.Cookies.Delete("UserEmail");
    }
}
