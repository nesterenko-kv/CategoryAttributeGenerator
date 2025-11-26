using System.Text.Json.Serialization;

namespace CategoryAttributeGenerator.Services.OpenAI.Data;

/// <summary>
///     Controls structured response formatting from OpenAI.
/// </summary>
public sealed class OpenAiResponseFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "json_object";
}