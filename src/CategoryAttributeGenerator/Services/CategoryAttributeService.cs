using System.Text.Json;
using CategoryAttributeGenerator.Models;
using CategoryAttributeGenerator.Services.OpenAI;
using CategoryAttributeGenerator.Services.OpenAI.Data;
using Microsoft.Extensions.Options;

namespace CategoryAttributeGenerator.Services;

/// <summary>
///     Default implementation of <see cref="ICategoryAttributeService" />.
///     Responsible for building prompts, calling OpenAI and parsing the results.
/// </summary>
public sealed class CategoryAttributeService : ICategoryAttributeService
{
    private const string SystemPrompt = "You are an expert in ecommerce product data. " +
                                        "Given a product subcategory name, you must return the three most important, " +
                                        "commonly used product attributes for that subcategory. " +
                                        "Return attributes that are useful for faceted navigation and product comparison.";

    private const string UserPromptFormat = """
                                            Subcategory name: "{0}"

                                            Return a JSON object in the following exact shape:

                                            {{
                                              "attributes": [
                                                "Attribute 1",
                                                "Attribute 2",
                                                "Attribute 3"
                                              ]
                                            }}

                                            Rules:
                                            - Always return exactly three attribute names.
                                            - Attribute names must be concise (max 3 words), in English, and human-readable.
                                            - Do not include explanations, comments, or additional fields.
                                            """;

    private readonly IOpenAiClient _openAiClient;
    private readonly ILogger<CategoryAttributeService> _logger;
    private readonly OpenAiOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public CategoryAttributeService(
        IOpenAiClient openAiClient,
        IOptions<OpenAiOptions> options,
        ILogger<CategoryAttributeService> logger
        )
    {
        _openAiClient = openAiClient;
        _logger = logger;
        _options = options.Value;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<IReadOnlyList<CategoryAttributesResultDto>> GenerateAttributesAsync(
        IReadOnlyList<CategoryGroupDto> categoryGroups,
        CancellationToken cancellationToken = default
        )
    {
        List<CategoryAttributesResultDto> results = [];

        // A very simple, sequential implementation is used here for clarity.
        // In a production setting we might fan out with controlled concurrency.
        foreach (CategoryGroupDto group in categoryGroups)
        {
            if (group.SubCategories is null or { Count: 0 })
            {
                _logger.LogInformation("Category group '{CategoryName}' has no subcategories; skipping.",
                    group.CategoryName);
                
                continue;
            }

            foreach (SubCategoryDto sub in group.SubCategories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                IReadOnlyList<string> attributes = await GetAttributesForSubCategoryAsync(sub, cancellationToken);
                results.Add(new CategoryAttributesResultDto(sub.CategoryId, attributes));
            }
        }

        return results;
    }
    
    private async Task<IReadOnlyList<string>> GetAttributesForSubCategoryAsync(
        SubCategoryDto subCategory,
        CancellationToken cancellationToken
        )
    {
        string userPrompt = string.Format(UserPromptFormat, subCategory.CategoryName);

        OpenAiChatCompletionRequest request = new()
        {
            Model = string.IsNullOrWhiteSpace(_options.Model)
                ? "gpt-4.1-mini"
                : _options.Model,
            Messages =
            [
                new OpenAiMessage("system", SystemPrompt),
                new OpenAiMessage("user", userPrompt)
            ],
            Temperature = 0.2f,
            ResponseFormat = new OpenAiResponseFormat { Type = "json_object" }
        };

        OpenAiChatCompletionResponse response =
            await _openAiClient.CreateChatCompletionAsync(request, cancellationToken);

        string? content = response.Choices.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
            throw new OpenAiException(
                $"OpenAI returned an empty response for subcategory '{subCategory.CategoryName}'.");

        try
        {
            AttributeListResponse? parsed = JsonSerializer.Deserialize<AttributeListResponse>(content, _jsonOptions);
            if (parsed is null || parsed.Attributes.Count != 3)
                throw new OpenAiException(
                    $"OpenAI response did not contain exactly three attributes for subcategory '{subCategory.CategoryName}'. Raw: {content}");

            return parsed.Attributes;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "Failed to parse OpenAI JSON for subcategory '{CategoryName}'. Raw content: {Content}",
                subCategory.CategoryName, content);

            throw new OpenAiException(
                $"Failed to parse attributes JSON for subcategory '{subCategory.CategoryName}'.", ex);
        }
    }
}