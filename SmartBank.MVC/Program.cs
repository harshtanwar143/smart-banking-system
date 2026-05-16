using Microsoft.AspNetCore.DataProtection;
using System.IO;
using Microsoft.AspNetCore.HttpOverrides;


var builder = WebApplication.CreateBuilder(args);

// ─── MVC ──────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ─── HTTP Client → SmartBank API ─────────────────────────────────────────────
builder.Services.AddHttpClient("SmartBankAPI", client =>
{
    var apiBase = builder.Configuration["ApiSettings:BaseUrl"];
    client.BaseAddress = new Uri(apiBase);
});

// ─── Session / Cookie ─────────────────────────────────────────────────────────
builder.Services.AddSession(options =>
{
    options.IdleTimeout        = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/tmp/keys"));

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                        ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name:    "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
