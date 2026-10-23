using System.ComponentModel.DataAnnotations;

namespace WayanadGo.Contracts.Destinations;

public sealed class CreateDestinationRequest
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(160, MinimumLength = 2)]
    public string Slug { get; init; } = string.Empty;

    [Required, StringLength(5000, MinimumLength = 10)]
    public string Description { get; init; } = string.Empty;

    [Required, StringLength(160, MinimumLength = 2)]
    public string Location { get; init; } = string.Empty;

    [Url, StringLength(1000)]
    public string? ImageUrl { get; init; }

    public bool IsPublished { get; init; } = true;
}
