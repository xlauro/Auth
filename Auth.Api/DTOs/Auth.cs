namespace Auth.Api.DTOs;


public record RegisterRequest(string Email, string Password);

public record RegisterResponse(Guid Id, string Email);

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token);
