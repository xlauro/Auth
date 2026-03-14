namespace Auth.Application.UseCases;

using Auth.Application.Exceptions;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Exceptions;
using FluentValidation;
using System.Security.Cryptography;
using System.Text;

public class RegisterUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<User> _validator;

    public RegisterUseCase(IUserRepository userRepository, IValidator<User> validator)
    {
        _userRepository = userRepository;
        _validator = validator;
    }

    public async Task<User> ExecuteAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ApplicationValidationException(["Email is required."]);
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ApplicationValidationException(["Password is required."]);
        }

        email = email.Trim();

        var existing = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existing != null)
        {
            throw new UserAlreadyExistsException(email);
        }

        var passwordHash = HashPassword(password);
        var now = DateTime.UtcNow;
        var user = new User(Guid.NewGuid(), email, passwordHash, now, now);

        var validationResult = _validator.Validate(user);
        if (!validationResult.IsValid)
        {
            throw new ApplicationValidationException(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return user;
    }

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}