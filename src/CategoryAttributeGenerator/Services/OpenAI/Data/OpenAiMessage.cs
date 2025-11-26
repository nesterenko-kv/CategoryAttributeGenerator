using System.Text.Json.Serialization;

namespace CategoryAttributeGenerator.Services.OpenAI.Data;

/// <summary>
///     Single chat message used by the OpenAI chat completions API.
/// </summary>
public sealed class OpenAiMessage
{
    public OpenAiMessage()
    {
    }

    public OpenAiMessage(
        string role,
        string content
        )
    {
        Role = role;
        Content = content;
    }

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}