using Auth.Domain.Entities;
using FluentValidation;

namespace Auth.Domain.Validation;

public class UserValidation : AbstractValidator<User>
{
    public UserValidation()
    {
        RuleFor(u => u.Email).NotEmpty()
            .EmailAddress()
            .WithMessage("Email is required");
        
        RuleFor(u => u.PasswordHash).NotEmpty()
            .MinimumLength(20)
            .WithMessage("Password is required to be encrypted");
    }
}