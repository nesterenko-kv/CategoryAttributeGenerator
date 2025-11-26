using System.Text.Json;
using CategoryAttributeGenerator.Models;
using CategoryAttributeGenerator.Services;
using CategoryAttributeGenerator.Services.OpenAI;
using Microsoft.AspNetCore.Mvc;

namespace CategoryAttributeGenerator.Controllers;

[ApiController]
[Route("api/category-attributes")]
public sealed class CategoryAttributesController : ControllerBase
{
    private readonly ICategoryAttributeService _categoryAttributeService;
    private readonly ILogger<CategoryAttributesController> _logger;
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public CategoryAttributesController(
        ICategoryAttributeService categoryAttributeService,
        ILogger<CategoryAttributesController> logger
        )
    {
        _categoryAttributeService = categoryAttributeService;
        _logger = logger;
    }

    /// <summary>
    ///     Accepts a JSON array of category groups, calls OpenAI for each subcategory
    ///     and returns the three most relevant product attributes per subcategory.
    /// </summary>
    /// <param name="categoryGroups">An array of category groups</param>
    /// <param name="cancellationToken"></param>
    [HttpPost]
    public async Task<IActionResult> GenerateAttributes(
        [FromBody] List<CategoryGroupDto> categoryGroups,
        CancellationToken cancellationToken
        )
    {
        // TODO: extract to middleware:
        string? traceIdFromHeader = HttpContext.Request.Headers["X-Trace-Id"].FirstOrDefault();
        string traceId = string.IsNullOrWhiteSpace(traceIdFromHeader)
            ? HttpContext.TraceIdentifier
            : traceIdFromHeader;

        using IDisposable? scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["TraceId"] = traceId
        });

        if (categoryGroups is null or { Count: 0 })
            return BadRequest(new ErrorResponse(
                "Input must be a non-empty JSON array of category groups."));

        try
        {
            IReadOnlyList<CategoryAttributesResultDto> result =
                await _categoryAttributeService.GenerateAttributesAsync(categoryGroups, cancellationToken);
            return Ok(result);
        }
        catch (OpenAiException ex)
        {
            // We surface a 502 here because the upstream dependency failed.
            _logger.LogError(ex, "OpenAI API call failed.");
            return StatusCode(StatusCodes.Status502BadGateway, new ErrorResponse(
                "Failed to generate attributes using OpenAI API.",
                [ex.Message]));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Request was cancelled by the client.");
            return StatusCode(StatusCodes.Status499ClientClosedRequest,
                new ErrorResponse("Request was cancelled."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while generating category attributes.");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse(
                "Unexpected server error while generating attributes.",
                [ex.Message]));
        }
    }
}