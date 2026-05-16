using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SmartBank.API.Services;
using SmartBank.Data.Repositories;
using SmartBank.Models.DTOs.Auth;
using SmartBank.Models.Entities;
using Xunit;

namespace SmartBank.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IAuthRepository> _authRepo;
    private readonly Mock<ILogger<AuthService>> _logger;
    private readonly IConfiguration _config;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _authRepo = new Mock<IAuthRepository>();
        _logger   = new Mock<ILogger<AuthService>>();

        var inMemConfig = new Dictionary<string, string?>
        {
            { "Jwt:SecretKey",   "SmartBank@SuperSecretKey#2025!ChangeThisInProduction" },
            { "Jwt:Issuer",      "SmartBank.API" },
            { "Jwt:Audience",    "SmartBank.MVC" },
            { "Jwt:ExpiryInMin", "1440" }
        };
        _config = new ConfigurationBuilder().AddInMemoryCollection(inMemConfig).Build();
        _service = new AuthService(_authRepo.Object, _config, _logger.Object);
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var dto = new RegisterDto
        {
            FirstName = "John",
            LastName  = "Doe",
            Email     = "john@example.com",
            Password  = "Password@123",
            PhoneNumber = "9876543210"
        };

        _authRepo.Setup(r => r.GetByEmail(dto.Email)).ReturnsAsync((User?)null);
        _authRepo.Setup(r => r.GetRoleByName("Customer")).ReturnsAsync(new Role { RoleId = 1, RoleName = "Customer" });
        _authRepo.Setup(r => r.CreateUser(It.IsAny<User>())).ReturnsAsync((User u) => { u.UserId = 1; return u; });

        // Act
        var result = await _service.RegisterAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.Email.Should().Be(dto.Email);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ShouldThrowException()
    {
        // Arrange
        var dto = new RegisterDto
        {
            FirstName = "John",
            LastName  = "Doe",
            Email     = "existing@example.com",
            Password  = "Password@123"
        };

        _authRepo.Setup(r => r.GetByEmail(dto.Email))
            .ReturnsAsync(new User { UserId = 99, Email = dto.Email });

        // Act & Assert
        var act = async () => await _service.RegisterAsync(dto);
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ShouldReturnToken()
    {
        // Arrange
        var email    = "john@example.com";
        var password = "Password@123";
        var hash     = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            UserId       = 1,
            Email        = email,
            PasswordHash = hash,
            FirstName    = "John",
            LastName     = "Doe",
            IsActive     = true,
            IsFrozen     = false,
            Role         = new Role { RoleId = 1, RoleName = "Customer" }
        };

        _authRepo.Setup(r => r.GetByEmail(email)).ReturnsAsync(user);
        _authRepo.Setup(r => r.UpdateUser(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.LoginAsync(new LoginDto { Email = email, Password = password });

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.Role.Should().Be("Customer");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldThrowException()
    {
        // Arrange
        var email = "john@example.com";
        var hash  = BCrypt.Net.BCrypt.HashPassword("RealPassword@123");

        var user = new User
        {
            UserId       = 1,
            Email        = email,
            PasswordHash = hash,
            FirstName    = "John",
            LastName     = "Doe",
            IsActive     = true,
            IsFrozen     = false,
            Role         = new Role { RoleId = 1, RoleName = "Customer" }
        };

        _authRepo.Setup(r => r.GetByEmail(email)).ReturnsAsync(user);
        _authRepo.Setup(r => r.UpdateUser(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act & Assert
        var act = async () => await _service.LoginAsync(new LoginDto { Email = email, Password = "WrongPass@99" });
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*Invalid*");
    }

    [Fact]
    public async Task Login_FrozenUser_ShouldThrowException()
    {
        // Arrange
        var email    = "frozen@example.com";
        var password = "Password@123";
        var hash     = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            UserId       = 2,
            Email        = email,
            PasswordHash = hash,
            FirstName    = "Frozen",
            LastName     = "User",
            IsActive     = true,
            IsFrozen     = true,
            Role         = new Role { RoleId = 1, RoleName = "Customer" }
        };

        _authRepo.Setup(r => r.GetByEmail(email)).ReturnsAsync(user);

        // Act & Assert
        var act = async () => await _service.LoginAsync(new LoginDto { Email = email, Password = password });
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*frozen*");
    }
}
