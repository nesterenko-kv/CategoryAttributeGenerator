using System.Text.Json.Serialization;

namespace CategoryAttributeGenerator.Services.OpenAI.Data;

public sealed class OpenAiChoice
{
    [JsonPropertyName("message")]
    public OpenAiMessage Message { get; set; } = null!;
}