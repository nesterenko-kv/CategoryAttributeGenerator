using CategoryAttributeGenerator.Models;

namespace CategoryAttributeGenerator.Services;

/// <summary>
/// Application service that orchestrates calling OpenAI and normalizing the attribute output.
/// </summary>
public interface ICategoryAttributeService
{
    /// <summary>
    /// For every subcategory in the provided category groups, asks OpenAI for the three most relevant attributes.
    /// </summary>
    /// <param name="categoryGroups">Hierarchy of categories and subcategories.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of attribute results per subcategory.</returns>
    Task<IReadOnlyList<CategoryAttributesResultDto>> GenerateAttributesAsync(
        IReadOnlyList<CategoryGroupDto> categoryGroups,
        CancellationToken cancellationToken = default);
}
