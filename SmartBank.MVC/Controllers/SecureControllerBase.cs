using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SmartBank.MVC.Controllers;

/// <summary>
/// Base controller for all authenticated MVC pages.
/// Provides JWT-attached HttpClient and redirects to login if no token.
/// </summary>
public abstract class SecureControllerBase : Controller
{
    protected readonly IHttpClientFactory HttpFactory;
    protected readonly ILogger Logger;

    protected static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected SecureControllerBase(IHttpClientFactory httpFactory, ILogger logger)
    {
        HttpFactory = httpFactory;
        Logger      = logger;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!Request.Cookies.ContainsKey("SmartBankToken"))
        {
            context.Result = RedirectToAction("Login", "Auth", new { returnUrl = Request.Path + Request.QueryString });
            return;
        }
        base.OnActionExecuting(context);
    }

    /// <summary>Build an HttpClient with the user's JWT attached.</summary>
    protected HttpClient ApiClient()
    {
        var client = HttpFactory.CreateClient("SmartBankAPI");
        var token  = Request.Cookies["SmartBankToken"];
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>GET helper that returns deserialized response or default.</summary>
    protected async Task<T?> ApiGetAsync<T>(string url) where T : class
    {
        try
        {
            var client = ApiClient();
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Logger.LogWarning("API GET {Url} returned {Status}", url, response.StatusCode);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Response.Cookies.Delete("SmartBankToken");
                }
                return null;
            }
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, JsonOpts);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "API GET {Url} failed", url);
            return null;
        }
    }

    /// <summary>POST helper. Returns the raw HttpResponseMessage so the caller can branch on status.</summary>
    protected async Task<(bool Success, T? Result, string? Error)> ApiPostAsync<T>(string url, object payload) where T : class
    {
        try
        {
            var client = ApiClient();
            var response = await client.PostAsJsonAsync(url, payload);
            var content  = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogWarning("API POST {Url} returned {Status}: {Body}", url, response.StatusCode, content);
                var errMsg = TryExtractMessage(content) ?? $"Request failed: {response.StatusCode}";
                return (false, null, errMsg);
            }

            var result = JsonSerializer.Deserialize<T>(content, JsonOpts);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "API POST {Url} failed", url);
            return (false, null, "Cannot reach the API server. Please try again.");
        }
    }

    /// <summary>PUT helper.</summary>
    protected async Task<(bool Success, T? Result, string? Error)> ApiPutAsync<T>(string url, object payload) where T : class
    {
        try
        {
            var client = ApiClient();
            var response = await client.PutAsJsonAsync(url, payload);
            var content  = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogWarning("API PUT {Url} returned {Status}: {Body}", url, response.StatusCode, content);
                var errMsg = TryExtractMessage(content) ?? $"Request failed: {response.StatusCode}";
                return (false, null, errMsg);
            }

            var result = JsonSerializer.Deserialize<T>(content, JsonOpts);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "API PUT {Url} failed", url);
            return (false, null, "Cannot reach the API server. Please try again.");
        }
    }

    private static string? TryExtractMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var m1)) return m1.GetString();
            if (doc.RootElement.TryGetProperty("Message", out var m2)) return m2.GetString();
        }
        catch { /* ignore – not JSON */ }
        return null;
    }

    protected int? CurrentUserId()
    {
        var raw = Request.Cookies["UserId"];
        return int.TryParse(raw, out var id) ? id : null;
    }

    protected string? CurrentUserRole() => Request.Cookies["UserRole"];
}
