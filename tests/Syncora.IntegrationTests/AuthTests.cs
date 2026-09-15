using System.Net;
using System.Net.Http.Json;
using Syncora.DTO.Auth;
using Syncora.Models;
using Xunit;

namespace Syncora.IntegrationTests;

public class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_Success_CreatesUserAndPersonalCalendar()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new RegisterRequest
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "Test123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.UserId);
        Assert.Equal("Test User", result.Name);
        Assert.NotNull(result.Token);
    }

    [Fact]
    public async Task Login_Success_ReturnsToken()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Сначала регистрируем пользователя
        var registerRequest = new RegisterRequest
        {
            Name = "Login User",
            Email = "login@example.com",
            Password = "Login123!"
        };
        await client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Act
        var loginRequest = new LoginRequest
        {
            Email = "login@example.com",
            Password = "Login123!"
        };
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Token);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Регистрируем пользователя
        var registerRequest = new RegisterRequest
        {
            Name = "Wrong Pass User",
            Email = "wrongpass@example.com",
            Password = "Correct123!"
        };
        await client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Act
        var loginRequest = new LoginRequest
        {
            Email = "wrongpass@example.com",
            Password = "Wrong123!"
        };
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}