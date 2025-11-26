using System.Text.Json.Serialization;

namespace CategoryAttributeGenerator.Models;

/// <summary>
///     Internal DTO used to deserialize the JSON that OpenAI returns for attributes.
/// </summary>
public sealed class AttributeListResponse
{
    [JsonPropertyName("attributes")] public List<string> Attributes { get; init; } = new();
}