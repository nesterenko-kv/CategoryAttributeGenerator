using System.Text.Json.Serialization;

namespace CategoryAttributeGenerator.Models;

/// <summary>
///     Standardized error response object for the API.
/// </summary>
public sealed class ErrorResponse
{
    public ErrorResponse(
        string message,
        IReadOnlyList<string>? details = null
        )
    {
        Message = message;
        Details = details;
    }

    [JsonPropertyName("message")]
    public string Message { get; }

    [JsonPropertyName("details")]
    public IReadOnlyList<string>? Details { get; }
}