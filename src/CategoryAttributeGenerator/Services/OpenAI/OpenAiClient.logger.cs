namespace CategoryAttributeGenerator.Services.OpenAI;

public partial class OpenAiClient
{
    [LoggerMessage(LogLevel.Error, "OpenAI returned non-success status code {statusCode}. Body snippet: {bodySnippet}")]
    partial void LogOpenAiReturnedNonSuccess(
        int statusCode,
        string bodySnippet
        );
}