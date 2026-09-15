using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Syncora.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Syncora.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Заменяем PostgreSQL на InMemory
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<SyncoraDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<SyncoraDbContext>(options =>
            {
                options.UseInMemoryDatabase("SyncoraTests");
            });

            // Настраиваем JwtSettings для тестов
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:SecretKey"] = "test-secret-key-for-jwt-token-generation-12345678",
                    ["JwtSettings:Issuer"] = "SyncoraTestAPI",
                    ["JwtSettings:Audience"] = "SyncoraTestClient"
                })
                .Build();

            services.AddSingleton<IConfiguration>(configuration);
        });

        builder.UseEnvironment("Testing");
    }

    public HttpClient CreateAuthenticatedClient(Guid userId, string email, string name)
    {
        var client = CreateClient();
        var token = GenerateTestToken(userId, email, name);
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private string GenerateTestToken(Guid userId, string email, string name)
    {
        var key = Encoding.UTF8.GetBytes("test-secret-key-for-jwt-token-generation-12345678");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", userId.ToString()),
                new Claim("email", email),
                new Claim("name", name),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}