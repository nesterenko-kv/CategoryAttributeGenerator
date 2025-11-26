using System.ComponentModel.DataAnnotations;

namespace CategoryAttributeGenerator.Services;

/// <summary>
/// Prompt configuration used when generating attributes for a product subcategory.
/// Moves all hard-coded prompt text into configuration.
/// </summary>
public sealed class CategoryPromptOptions
{
    /// <summary>
    ///     System message that defines the assistant role and global behavior.
    /// </summary>
    [Required]
    public string SystemPrompt { get; set; } =
        "You are an expert in ecommerce product data. " +
        "Given a product subcategory name, you must return the three most important, " +
        "commonly used product attributes for that subcategory. " +
        "Return attributes that are useful for faceted navigation and product comparison.";

    /// <summary>
    ///     User message template used for each subcategory.
    ///     The placeholder <c>{SubcategoryName}</c> is replaced with the actual subcategory name.
    /// </summary>
    [Required]
    public string UserPromptTemplate { get; set; } =
        """
        Subcategory name: "{SubcategoryName}"

        Return a JSON object in the following exact shape:

        {
          "attributes": [
            "Attribute 1",
            "Attribute 2",
            "Attribute 3"
          ]
        }

        Rules:
        - Always return exactly three attribute names.
        - Attribute names must be concise (max 3 words), in English, and human-readable.
        - Do not include explanations, comments, or additional fields.
        """;
}