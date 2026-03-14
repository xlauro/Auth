using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Auth.Application.Exceptions;
using Auth.Application.Interfaces;
using Auth.Application.UseCases;
using Auth.Domain.Entities;
using Auth.Domain.Exceptions;
using Auth.Domain.Validation;
using Auth.Infrastructure.Data;
using Auth.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Auth.Tests;

public class UseCasesTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    [Fact]
    public async Task RegisterUseCase_CreatesUser()
    {
        await using var context = CreateInMemoryContext();
        var repo = new UserRepository(context);
        var validator = new UserValidation();
        var useCase = new RegisterUseCase(repo, validator);

        var user = await useCase.ExecuteAsync("test@example.com", "P@ssword123!", CancellationToken.None);

        Assert.Equal("test@example.com", user.Email);
        Assert.Single(context.Users);
    }

    [Fact]
    public async Task RegisterUseCase_ExistingEmail_Throws()
    {
        await using var context = CreateInMemoryContext();
        var existing = new User(Guid.NewGuid(), "test@example.com", HashPassword("Secret1"), DateTime.UtcNow, DateTime.UtcNow);
        await context.Users.AddAsync(existing);
        await context.SaveChangesAsync();

        var repo = new UserRepository(context);
        var validator = new UserValidation();
        var useCase = new RegisterUseCase(repo, validator);

        await Assert.ThrowsAsync<UserAlreadyExistsException>(() => useCase.ExecuteAsync("test@example.com", "P@ssword123!", CancellationToken.None));
    }

    [Fact]
    public async Task LoginUseCase_ReturnsToken()
    {
        await using var context = CreateInMemoryContext();
        var hashed = HashPassword("P@ssword123!");
        var existing = new User(Guid.NewGuid(), "test@example.com", hashed, DateTime.UtcNow, DateTime.UtcNow);
        await context.Users.AddAsync(existing);
        await context.SaveChangesAsync();

        var repo = new UserRepository(context);
        var tokenGenerator = new StubTokenGenerator();
        var useCase = new LoginUseCase(repo, tokenGenerator);

        var token = await useCase.ExecuteAsync("test@example.com", "P@ssword123!", CancellationToken.None);

        Assert.Equal(StubTokenGenerator.TokenValue, token);
    }

    [Fact]
    public async Task LoginUseCase_InvalidPassword_Throws()
    {
        await using var context = CreateInMemoryContext();
        var hashed = HashPassword("CorrectPassword");
        var existing = new User(Guid.NewGuid(), "test@example.com", hashed, DateTime.UtcNow, DateTime.UtcNow);
        await context.Users.AddAsync(existing);
        await context.SaveChangesAsync();

        var repo = new UserRepository(context);
        var tokenGenerator = new StubTokenGenerator();
        var useCase = new LoginUseCase(repo, tokenGenerator);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => useCase.ExecuteAsync("test@example.com", "WrongPassword", CancellationToken.None));
    }

    private sealed class StubTokenGenerator : IJwtTokenGenerator
    {
        public const string TokenValue = "stub-token";

        public string GenerateToken(User user) => TokenValue;
    }
}


