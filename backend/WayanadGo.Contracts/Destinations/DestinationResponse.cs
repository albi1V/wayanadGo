namespace WayanadGo.Contracts.Destinations;

public sealed record DestinationResponse(
    int Id,
    string Name,
    string Slug,
    string Description,
    string Location,
    string? ImageUrl,
    bool IsPublished);
