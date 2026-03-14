using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Auth.Api.DTOs;
using Auth.Domain.Entities;
using Auth.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Auth.Tests.Integration;

public class AuthApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"ConnectionStrings:DefaultConnection", "DataSource=:memory:"},
                {"Jwt:Issuer", "AuthApi.Test"},
                {"Jwt:Audience", "AuthApi.Test.Client"},
                {"Jwt:Secret", "TESTING_SECRET_KEY_1234567890_ABCDEF!!"},
                {"Jwt:ExpiresMinutes", "60"}
            });
        });
        builder.ConfigureServices(services =>
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(AppDbContext));
            services.AddSingleton(connection);

            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                var sqliteConnection = sp.GetRequiredService<SqliteConnection>();
                options.UseSqlite(sqliteConnection);
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });
    }
}

public class AuthIntegrationTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public AuthIntegrationTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RegisterAndLogin_FullFlow_ReturnsToken()
    {
        var client = _factory.CreateClient();
        var email = $"user_{Guid.NewGuid():N}@example.com";
        const string password = "P@ssword123";

        var registerResponse = await client.PostAsJsonAsync("/auth/register", new RegisterRequest(email, password));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginRequest(email, password));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var payload = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Token));
    }
}

