using System.Collections.Concurrent;
using System.Text.Json;
using CategoryAttributeGenerator.Models;
using CategoryAttributeGenerator.Services.OpenAI;
using CategoryAttributeGenerator.Services.OpenAI.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CategoryAttributeGenerator.Services;

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
    private readonly CategoryAttributesOptions _attributesOptions;
    private readonly JsonSerializerOptions _jsonOptions;

    public CategoryAttributeService(
        IOpenAiClient openAiClient,
        IOptions<OpenAiOptions> openAiOptions,
        IOptions<CategoryPromptOptions> promptOptions,
        IOptions<CategoryAttributesOptions> attributesOptions,
        ILogger<CategoryAttributeService> logger,
        IMemoryCache cache
        )
    {
        _openAiClient = openAiClient;
        _logger = logger;
        _cache = cache;
        _openAiOptions = openAiOptions.Value;
        _promptOptions = promptOptions.Value;
        _attributesOptions = attributesOptions.Value;

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

        ParallelOptions options = new()
        {
            MaxDegreeOfParallelism = _attributesOptions.MaxConcurrency,
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
    
    private static string SanitizeCategoryName(string name)
    {
        string trimmed = name.Trim();

        if (trimmed.Length > 200)
        {
            trimmed = trimmed[..200];
        }

        trimmed = trimmed.ReplaceLineEndings(" ");

        return trimmed;
    }
    
    private async Task<IReadOnlyList<string>> GetAttributesForSubCategoryAsync(
        SubCategoryDto subCategory,
        CancellationToken cancellationToken
        )
    {
        int categoryId = subCategory.CategoryId;
        string sanitizedName = SanitizeCategoryName(subCategory.CategoryName);

        string cacheKey = BuildCacheKey(categoryId, sanitizedName);

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<string>? cachedAttributes)
            && cachedAttributes is not null)
        {
            LogCacheHitForSubcategory(sanitizedName, categoryId);

            return cachedAttributes;
        }

        LogCacheMissForSubcategory(sanitizedName, categoryId);
        
        // Take prompts from configuration (with sensible defaults)
        string systemPrompt = _promptOptions.SystemPrompt;

        string userPrompt = _promptOptions
            .UserPromptTemplate
            .Replace("{SubcategoryName}", sanitizedName);

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
            Temperature = _openAiOptions.Temperature,
            ResponseFormat = new OpenAiResponseFormat { Type = "json_object" }
        };

        OpenAiChatCompletionResponse response =
            await _openAiClient.CreateChatCompletionAsync(request, cancellationToken);

        OpenAiChoice? firstChoice = response.Choices.FirstOrDefault();
        string? content = firstChoice?.Message.Content;
        if (string.IsNullOrWhiteSpace(content))
            throw new OpenAiException(
                $"OpenAI returned an empty response for subcategory '{sanitizedName}'.");
        
        List<string> attributes;
        try
        {
            AttributeListResponse? parsed = JsonSerializer.Deserialize<AttributeListResponse>(content, _jsonOptions);
            
            if (parsed?.Attributes is not { Count: 3 })
                throw new OpenAiException(
                    $"OpenAI response did not contain exactly three attributes for subcategory '{sanitizedName}'. Raw: {content}");
            
            if (parsed.Attributes.Any(a =>
                    string.IsNullOrWhiteSpace(a) ||
                    a.Length > 50 ||
                    a.Any(char.IsControl)))
            {
                throw new OpenAiException(
                    $"OpenAI response contained invalid attribute values for subcategory '{subCategory.CategoryName}'. Raw: {content}");
            }
            
            attributes = parsed.Attributes;
        }
        catch (JsonException ex)
        {
            LogFailedToParseJson(ex, sanitizedName, content);

            throw new OpenAiException(
                $"Failed to parse attributes JSON for subcategory '{sanitizedName}'.", ex);
        }

        _cache.Set(
            cacheKey,
            attributes,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_attributesOptions.CacheDurationMinutes),
                Size = attributes.Count
            });

        return attributes;
    }

    private static string BuildCacheKey(
        int categoryId,
        string categoryName
        )
    {
        // Cache key is based on category id + name. In a real system you might also
        // include locale, model name, or other parameters that affect the output.
        return $"category-attributes:{categoryId}:{categoryName}";
    }
}