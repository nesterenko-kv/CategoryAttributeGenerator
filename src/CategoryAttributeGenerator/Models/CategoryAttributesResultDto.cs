using System.Text.Json.Serialization;

namespace CategoryAttributeGenerator.Models;

/// <summary>
///     Represents the output for a single subcategory.
/// </summary>
public sealed class CategoryAttributesResultDto
{
    public CategoryAttributesResultDto(
        int categoryId,
        IReadOnlyList<string> attributes
        )
    {
        CategoryId = categoryId;
        Attributes = attributes;
    }

    [JsonPropertyName("categoryId")] public int CategoryId { get; init; }

    [JsonPropertyName("attributes")] public IReadOnlyList<string> Attributes { get; init; }
}