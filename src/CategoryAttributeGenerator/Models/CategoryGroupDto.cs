using System.Text.Json.Serialization;

namespace CategoryAttributeGenerator.Models;

/// <summary>
///     Represents a top-level category group that contains multiple subcategories.
///     This is the shape of the input JSON.
/// </summary>
public sealed class CategoryGroupDto
{
    [JsonPropertyName("categoryName")]
    public string CategoryName { get; init; } = string.Empty;

    [JsonPropertyName("subCategories")]
    public List<SubCategoryDto> SubCategories { get; init; } = [];
}