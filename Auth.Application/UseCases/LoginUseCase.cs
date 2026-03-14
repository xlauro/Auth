using Auth.Application.Exceptions;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Exceptions;
using System.Security.Cryptography;
using System.Text;

namespace Auth.Application.UseCases;

public class LoginUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginUseCase(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<string> ExecuteAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new ApplicationValidationException(["Email and password are required."]);
        }

        email = email.Trim();

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            throw new UserNotFoundException(email);
        }

        var passwordHash = HashPassword(password);
        if (!string.Equals(user.PasswordHash, passwordHash, StringComparison.Ordinal))
        {
            throw new ApplicationValidationException(["Invalid credentials."]);
        }

        return _jwtTokenGenerator.GenerateToken(user);
    }

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
