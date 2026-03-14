using Auth.Domain.Entities;

namespace Auth.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
