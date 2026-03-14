using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Auth.Api.Controllers;
using Auth.Api.DTOs;
using Auth.Application.Exceptions;
using Auth.Application.Interfaces;
using Auth.Application.UseCases;
using Auth.Domain.Entities;
using Auth.Domain.Exceptions;
using Auth.Domain.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Auth.Tests;

public class AuthControllerTests
{
    private readonly IValidator<User> _validator = new UserValidation();

    [Fact]
    public async Task Register_ReturnsOk_WithResponse()
    {
        var repo = new FakeUserRepository();
        var registerUseCase = new RegisterUseCase(repo, _validator);
        var loginUseCase = new LoginUseCase(repo, new StubTokenGenerator());
        var controller = new AuthController(registerUseCase, loginUseCase);

        var result = await controller.Register(new RegisterRequest("test@example.com", "password123"), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<RegisterResponse>(okResult.Value);
        Assert.Equal("test@example.com", response.Email);
    }

    [Fact]
    public async Task Register_ExistingEmail_ReturnsConflict()
    {
        var repo = new FakeUserRepository();
        var existing = new User(Guid.NewGuid(), "test@example.com", "hash", DateTime.UtcNow, DateTime.UtcNow);
        await repo.AddAsync(existing, CancellationToken.None);
        var registerUseCase = new RegisterUseCase(repo, _validator);
        var loginUseCase = new LoginUseCase(repo, new StubTokenGenerator());
        var controller = new AuthController(registerUseCase, loginUseCase);

        var result = await controller.Register(new RegisterRequest("test@example.com", "password123"), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Register_InvalidRequest_ReturnsBadRequest()
    {
        var repo = new FakeUserRepository();
        var registerUseCase = new RegisterUseCase(repo, _validator);
        var loginUseCase = new LoginUseCase(repo, new StubTokenGenerator());
        var controller = new AuthController(registerUseCase, loginUseCase);

        var result = await controller.Register(new RegisterRequest("", ""), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_ReturnsToken_OnSuccess()
    {
        var repo = new FakeUserRepository();
        var user = new User(Guid.NewGuid(), "test@example.com", StubTokenGenerator.ExpectedHash, DateTime.UtcNow, DateTime.UtcNow);
        await repo.AddAsync(user, CancellationToken.None);
        var registerUseCase = new RegisterUseCase(repo, _validator);
        var loginUseCase = new LoginUseCase(repo, new StubTokenGenerator());
        var controller = new AuthController(registerUseCase, loginUseCase);

        var result = await controller.Login(new LoginRequest("test@example.com", StubTokenGenerator.ExpectedPassword), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<LoginResponse>(okResult.Value);
        Assert.Equal(StubTokenGenerator.TokenValue, response.Token);
    }

    [Fact]
    public async Task Login_UnknownUser_ReturnsNotFound()
    {
        var repo = new FakeUserRepository();
        var registerUseCase = new RegisterUseCase(repo, _validator);
        var loginUseCase = new LoginUseCase(repo, new StubTokenGenerator());
        var controller = new AuthController(registerUseCase, loginUseCase);

        var result = await controller.Login(new LoginRequest("missing@example.com", "any"), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users = [];

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.Find(u => u.Email == email));
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubTokenGenerator : IJwtTokenGenerator
    {
        public const string TokenValue = "stub-token";
        public const string ExpectedPassword = "P@ssword123";
        public const string ExpectedHash = "62A39DF87B501AD40B6FC145820756CCEDCAB952C64626968E83CCBAE5BEAE63"; // SHA256 of ExpectedPassword
        public string GenerateToken(User user) => TokenValue;
    }
}

