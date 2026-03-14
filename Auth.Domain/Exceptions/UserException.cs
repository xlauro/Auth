namespace Auth.Domain.Exceptions;

public class UserException : Exception
{
    public UserException()
    {
    }

    public UserException(string message) : base(message)
    {
    }

    public UserException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}

public sealed class UserNotFoundException : UserException
{
    public UserNotFoundException()
        : base("User was not found.")
    {
    }

    public UserNotFoundException(Guid userId)
        : base($"User with id '{userId}' was not found.")
    {
    }

    public UserNotFoundException(string message) : base(message)
    {
    }

    public UserNotFoundException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}

public sealed class UserAlreadyExistsException : UserException
{
    public UserAlreadyExistsException()
        : base("User already exists.")
    {
    }

    public UserAlreadyExistsException(string email)
        : base($"User with email '{email}' already exists.")
    {
    }

    public UserAlreadyExistsException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}
