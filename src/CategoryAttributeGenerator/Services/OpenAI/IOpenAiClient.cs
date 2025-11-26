using CategoryAttributeGenerator.Services.OpenAI.Data;

namespace CategoryAttributeGenerator.Services.OpenAI;

/// <summary>
///     Typed abstraction over the HTTP client used to call OpenAI.
/// </summary>
public interface IOpenAiClient
{
    Task<OpenAiChatCompletionResponse> CreateChatCompletionAsync(
        OpenAiChatCompletionRequest request,
        CancellationToken cancellationToken = default
        );
}