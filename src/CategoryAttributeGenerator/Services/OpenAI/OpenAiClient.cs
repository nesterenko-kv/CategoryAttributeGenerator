using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CategoryAttributeGenerator.Services.OpenAI.Data;
using Microsoft.Extensions.Options;

namespace CategoryAttributeGenerator.Services.OpenAI;

/// <summary>
///     Default HttpClient-based implementation for calling OpenAI's chat completions API.
/// </summary>
public sealed partial class OpenAiClient : IOpenAiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<OpenAiClient> _logger;
    private readonly OpenAiOptions _options;

    public OpenAiClient(
        HttpClient httpClient,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiClient> logger
        )
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<OpenAiChatCompletionResponse> CreateChatCompletionAsync(
        OpenAiChatCompletionRequest request,
        CancellationToken cancellationToken = default
        )
    {
        string? apiKey = ResolveApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new OpenAiException(
                "OpenAI API key is not configured. Set OpenAI:ApiKey or the OPENAI_API_KEY environment variable.");

        string url = string.IsNullOrWhiteSpace(_options.ApiUrl)
            ? "https://api.openai.com/v1/chat/completions"
            : _options.ApiUrl;

        using HttpRequestMessage httpRequest = new(HttpMethod.Post, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        string payload = JsonSerializer.Serialize(request, _jsonOptions);
        httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string snippet = responseContent.Length > 500
                ? responseContent[..500]
                : responseContent;

            LogOpenAiReturnedNonSuccess((int)response.StatusCode, snippet);

            throw new OpenAiException(
                $"OpenAI request failed with status {(int)response.StatusCode} ({response.StatusCode}). " +
                "Check logs for more details.");
        }

        OpenAiChatCompletionResponse? result = JsonSerializer.Deserialize<OpenAiChatCompletionResponse>(responseContent, _jsonOptions);
        
        if (result is null)
        {
            throw new OpenAiException("Failed to deserialize OpenAI response.");
        }

        return result;
    }

    private string? ResolveApiKey()
    {
        // Priority: config value, then environment variable.
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return _options.ApiKey;
        }

        string? fromEnv = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }
        
        return null;
    }
}