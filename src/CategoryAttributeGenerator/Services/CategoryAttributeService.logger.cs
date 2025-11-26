namespace CategoryAttributeGenerator.Services;

public partial class CategoryAttributeService
{
    [LoggerMessage(LogLevel.Information, "Category group '{CategoryName}' has no subcategories; skipping.")]
    partial void LogCategoryHasNoSubcategories(
        string categoryName
        );

    [LoggerMessage(LogLevel.Warning, "Failed to parse OpenAI JSON for subcategory '{CategoryName}'. Raw content: {Content}")]
    partial void LogFailedToParseJson(
        Exception exception,
        string categoryName,
        string content
        );
}