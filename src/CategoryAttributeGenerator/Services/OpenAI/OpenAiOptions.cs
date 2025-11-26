namespace CategoryAttributeGenerator.Services.OpenAI;

/// <summary>
///     Settings for connecting to the OpenAI API.
/// </summary>
public sealed class OpenAiOptions
{
    /// <summary>
    ///     API key for authenticating with OpenAI.
    ///     Can be provided via configuration or environment variables.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    ///     Model name, e.g. gpt-4.1-mini.
    /// </summary>
    public string Model { get; set; } = "gpt-4.1-mini";

    /// <summary>
    ///     Base URL for the chat completions endpoint.
    /// </summary>
    public string ApiUrl { get; set; } = "https://api.openai.com/v1/chat/completions";
}