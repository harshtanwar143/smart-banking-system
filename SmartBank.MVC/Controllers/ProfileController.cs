using Microsoft.AspNetCore.Mvc;
using SmartBank.Models.DTOs.Profile;

namespace SmartBank.MVC.Controllers;

public class ProfileController : SecureControllerBase
{
    public ProfileController(IHttpClientFactory http, ILogger<ProfileController> logger)
        : base(http, logger) { }

    // GET /Profile
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var profile = await ApiGetAsync<ProfileDto>("api/profile");
        if (profile is null)
        {
            TempData["ErrorMessage"] = "Could not load your profile.";
            return RedirectToAction("Index", "Dashboard");
        }
        return View(profile);
    }

    // GET /Profile/Edit
    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var profile = await ApiGetAsync<ProfileDto>("api/profile");
        if (profile is null)
        {
            TempData["ErrorMessage"] = "Could not load your profile.";
            return RedirectToAction(nameof(Index));
        }

        var update = new UpdateProfileDto
        {
            Name    = profile.Name,
            Phone   = profile.Phone ?? string.Empty,
            Address = profile.Address ?? string.Empty
        };
        ViewBag.Email = profile.Email;
        return View(update);
    }

    // POST /Profile/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateProfileDto model)
    {
        if (!ModelState.IsValid) return View(model);

        var (ok, _, error) = await ApiPutAsync<ProfileResponseDto>("api/profile", model);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "Could not update profile.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}
