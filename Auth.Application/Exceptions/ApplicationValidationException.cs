namespace Auth.Application.Exceptions;

public class ApplicationValidationException : Exception
{
    public IReadOnlyCollection<string> Errors { get; }

    public ApplicationValidationException(IEnumerable<string> errors)
        : base("Validation failed for one or more properties.")
    {
        Errors = errors.ToArray();
    }
}
