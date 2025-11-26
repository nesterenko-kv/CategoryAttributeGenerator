using System.Text.Json.Serialization;

namespace CategoryAttributeGenerator.Services.OpenAI.Data;

/// <summary>
///     Response shape for OpenAI chat completions API (simplified).
/// </summary>
public sealed class OpenAiChatCompletionResponse
{
    [JsonPropertyName("choices")]
    public List<OpenAiChoice> Choices { get; set; } = [];
}