using System.Text.Json.Serialization;

namespace CategoryAttributeGenerator.Services.OpenAI.Data;

/// <summary>
///     Request payload for OpenAI's chat completions API.
///     Only the fields relevant for this exercise are included.
/// </summary>
public sealed class OpenAiChatCompletionRequest
{
    /// <summary>
    ///     Identifier of the model to use for this request
    ///     (for example, <c>gpt-4o-mini</c> ).
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    ///     Ordered list of chat messages that define the conversation
    ///     context (system, user, assistant, etc.).
    /// </summary>
    [JsonPropertyName("messages")]
    public List<OpenAiMessage> Messages { get; set; } = [];

    /// <summary>
    ///     Controls randomness in the model output.
    ///     Lower values (for example, 0–0.3) make responses more
    ///     deterministic, higher values make them more diverse.
    /// </summary>
    [JsonPropertyName("temperature")]
    public float Temperature { get; set; } = 0.2f;

    /// <summary>
    ///     Optional structured response format description.
    ///     When set (for example, to type <c>json_object</c>),
    ///     the model is instructed to produce output in that format.
    /// </summary>
    [JsonPropertyName("response_format")]
    public OpenAiResponseFormat? ResponseFormat { get; set; }
}