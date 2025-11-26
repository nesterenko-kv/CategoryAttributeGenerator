namespace CategoryAttributeGenerator.Services;

public sealed class CategoryAttributesOptions
{
    public int MaxConcurrency { get; set; } = 5;
    public int CacheDurationMinutes { get; set; } = 60;
}