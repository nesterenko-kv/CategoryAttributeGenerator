using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using CategoryAttributeGenerator.Models;
using CategoryAttributeGenerator.Services.OpenAI;
using CategoryAttributeGenerator.Services.OpenAI.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

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

/// <summary>
///     Default implementation of <see cref="ICategoryAttributeService" />.
///     Responsible for building prompts, calling OpenAI and parsing the results.
/// </summary>
public sealed partial class CategoryAttributeService : ICategoryAttributeService
{
    private readonly IOpenAiClient _openAiClient;
    private readonly ILogger<CategoryAttributeService> _logger;
    private readonly IMemoryCache _cache;
    private readonly OpenAiOptions _openAiOptions;
    private readonly CategoryPromptOptions _promptOptions;
    private readonly JsonSerializerOptions _jsonOptions;

    public CategoryAttributeService(
        IOpenAiClient openAiClient,
        IOptions<OpenAiOptions> openAiOptions,
        IOptions<CategoryPromptOptions> promptOptions,
        ILogger<CategoryAttributeService> logger,
        IMemoryCache cache
        )
    {
        _openAiClient = openAiClient;
        _logger = logger;
        _cache = cache;
        _openAiOptions = openAiOptions.Value;
        _promptOptions = promptOptions.Value;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }
    public async Task<IReadOnlyList<CategoryAttributesResultDto>> GenerateAttributesAsync(
        IReadOnlyList<CategoryGroupDto> categoryGroups,
        CancellationToken cancellationToken = default)
    {
        foreach (CategoryGroupDto group in categoryGroups)
        {
            if (group.SubCategories is null or { Count: 0 })
            {
                LogCategoryHasNoSubcategories(group.CategoryName);
            }
        }

        SubCategoryDto[] subCategories = categoryGroups
            .Where(g => g.SubCategories is { Count: > 0 })
            .SelectMany(g => g.SubCategories)
            .ToArray();

        if (subCategories.Length == 0)
        {
            return [];
        }

        // better to extract to config
        const int maxConcurrency = 5;
        ParallelOptions options = new()
        {
            MaxDegreeOfParallelism = maxConcurrency,
            CancellationToken = cancellationToken
        };

        ConcurrentBag<CategoryAttributesResultDto> results = [];

        await Parallel.ForEachAsync(subCategories, options, async (sub, ct) =>
        {
            IReadOnlyList<string> attributes = await GetAttributesForSubCategoryAsync(sub, ct)
                .ConfigureAwait(false);

            results.Add(new CategoryAttributesResultDto(sub.CategoryId, attributes));
        });

        return results.OrderBy(x => x.CategoryId).ToArray();
    }
    
    private async Task<IReadOnlyList<string>> GetAttributesForSubCategoryAsync(
        SubCategoryDto subCategory,
        CancellationToken cancellationToken
        )
    { 
        string cacheKey = BuildCacheKey(subCategory);

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<string>? cachedAttributes)
            && cachedAttributes is not null)
        {
            LogCacheHitForSubcategory(subCategory.CategoryName, subCategory.CategoryId);

            return cachedAttributes;
        }

        LogCacheMissForSubcategory(subCategory.CategoryName, subCategory.CategoryId);
        
        // Take prompts from configuration (with sensible defaults)
        string systemPrompt = _promptOptions.SystemPrompt;

        string userPrompt = _promptOptions
            .UserPromptTemplate
            .Replace("{SubcategoryName}", subCategory.CategoryName);

        OpenAiChatCompletionRequest request = new()
        {
            Model = string.IsNullOrWhiteSpace(_openAiOptions.Model)
                ? "gpt-4.1-mini"
                : _openAiOptions.Model,
            Messages =
            [
                new OpenAiMessage("system", systemPrompt),
                new OpenAiMessage("user", userPrompt)
            ],
            Temperature = 0.2f,
            ResponseFormat = new OpenAiResponseFormat { Type = "json_object" }
        };

        OpenAiChatCompletionResponse response =
            await _openAiClient.CreateChatCompletionAsync(request, cancellationToken);

        OpenAiChoice? firstChoice = response.Choices.FirstOrDefault();
        string? content = firstChoice?.Message.Content;
        if (string.IsNullOrWhiteSpace(content))
            throw new OpenAiException(
                $"OpenAI returned an empty response for subcategory '{subCategory.CategoryName}'.");
        
        List<string> attributes;
        try
        {
            AttributeListResponse? parsed = JsonSerializer.Deserialize<AttributeListResponse>(content, _jsonOptions);
            
            if (parsed?.Attributes is not { Count: 3 })
                throw new OpenAiException(
                    $"OpenAI response did not contain exactly three attributes for subcategory '{subCategory.CategoryName}'. Raw: {content}");

            attributes = parsed.Attributes;
        }
        catch (JsonException ex)
        {
            LogFailedToParseJson(ex, subCategory.CategoryName, content);

            throw new OpenAiException(
                $"Failed to parse attributes JSON for subcategory '{subCategory.CategoryName}'.", ex);
        }

        _cache.Set(
            cacheKey,
            attributes,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                Size = attributes.Count
            });

        return attributes;
    }

    private static string BuildCacheKey(
        SubCategoryDto subCategory
        )
    {
        // Cache key is based on category id + name. In a real system you might also
        // include locale, model name, or other parameters that affect the output.
        return $"category-attributes:{subCategory.CategoryId}:{subCategory.CategoryName}";
    }
}