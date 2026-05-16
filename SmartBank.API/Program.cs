using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartBank.API.Helpers;
using SmartBank.API.Middleware;
using SmartBank.API.Services;
using SmartBank.API.Services.Interfaces;
using SmartBank.API.Validators;
using SmartBank.Data.Context;
using SmartBank.Data.Repositories;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<SmartOnlineBankingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SmartBankDB")));

// ─── Repositories & Services ──────────────────────────────────────────────────
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService,    AuthService>();
builder.Services.AddSingleton<JwtHelper>();

// Sprint 3 – Transactions
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransactionService,    TransactionService>();

// Sprint 4 – Loans, Support, Notifications
builder.Services.AddScoped<ILoanRepository,         LoanRepository>();
builder.Services.AddScoped<ILoanService,            LoanService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService,    NotificationService>();
builder.Services.AddScoped<ISupportRepository,      SupportRepository>();
builder.Services.AddScoped<ISupportService,         SupportService>();

// Sprint 5 – Admin
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAdminService,    AdminService>();

// ─── JWT Authentication ───────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey   = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JwtSettings:SecretKey is missing.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtSettings["Issuer"],
        ValidAudience            = jwtSettings["Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew                = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ─── Fluent Validation ────────────────────────────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<DepositRequestValidator>();

// ─── Controllers ──────────────────────────────────────────────────────────────
builder.Services.AddControllers(options =>
{
    options.Filters.Add<SmartBank.API.Filters.FluentValidationActionFilter>();
});

// ─── Swagger / OpenAPI ────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "SmartBank API",
        Version     = "v1",
        Description = "SmartBank Digital Banking Platform – Full API (Auth, Accounts, Transactions, Loans, Support, Admin)"
    });

    // JWT auth in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter: Bearer {your-token}",
        Name        = "Authorization",
        In          = ParameterLocation.Header,
        Type        = SecuritySchemeType.ApiKey,
        Scheme      = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ─── CORS (allow MVC front-end) ───────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMVC", policy =>
        policy.WithOrigins("https://localhost:7100", "http://localhost:5100")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// ─── Middleware Pipeline ──────────────────────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartBank API v1"));
}

app.UseHttpsRedirection();
app.UseCors("AllowMVC");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ─── Database First: Ensure database exists (no migrations) ─────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SmartOnlineBankingDbContext>();
    db.Database.EnsureCreated(); // Uses existing SQL-created DB schema
}

app.Run();
