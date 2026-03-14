namespace Auth.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get;  set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected User()
    {
    }

    public User(Guid id, string email, string passwordHash, DateTime createdAt, DateTime updatedAt)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}