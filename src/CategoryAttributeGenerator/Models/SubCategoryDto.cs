using System.Text.Json.Serialization;

namespace CategoryAttributeGenerator.Models;

/// <summary>
///     Represents a leaf subcategory for which we want to generate attributes.
/// </summary>
public sealed class SubCategoryDto
{
    [JsonPropertyName("categoryId")]
    public int CategoryId { get; init; }

    [JsonPropertyName("categoryName")]
    public string CategoryName { get; init; } = string.Empty;
}