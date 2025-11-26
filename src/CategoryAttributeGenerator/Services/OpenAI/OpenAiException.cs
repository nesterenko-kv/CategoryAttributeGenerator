namespace CategoryAttributeGenerator.Services.OpenAI;

/// <summary>
///     Lightweight domain-specific exception type for OpenAI failures.
/// </summary>
public sealed class OpenAiException : Exception
{
    public OpenAiException(
        string message
        ) : base(message)
    {
    }

    public OpenAiException(
        string message,
        Exception innerException
        ) : base(message, innerException)
    {
    }
}