using WayanadGo.Application.Interfaces;
using WayanadGo.Contracts.Destinations;
using WayanadGo.Application.Entities;

namespace WayanadGo.Application.Services;

public sealed class DestinationService(IDestinationRepository repository)
{
    public async Task<IReadOnlyList<DestinationResponse>> SearchAsync(string? search, CancellationToken ct)
    {
        var rows = await repository.GetPublishedAsync(search, ct);
        return rows.Select(ToResponse).ToArray();
    }

    public async Task<DestinationResponse?> GetPublishedByIdAsync(int id, CancellationToken ct)
    {
        var row = await repository.GetByIdAsync(id, ct);
        return row is { IsPublished: true } ? ToResponse(row) : null;
    }

    public async Task<DestinationResponse> CreateAsync(CreateDestinationRequest request, CancellationToken ct)
    {
        var entity = new Destination
        {
            Name = request.Name.Trim(),
            Slug = request.Slug.Trim().ToLowerInvariant(),
            Description = request.Description.Trim(),
            Location = request.Location.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            IsPublished = request.IsPublished
        };
        var created = await repository.AddAsync(entity, ct);
        return ToResponse(created);
    }

    public async Task<DestinationResponse?> UpdateAsync(int id, CreateDestinationRequest request, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return null;
        entity.Name = request.Name.Trim();
        entity.Slug = request.Slug.Trim().ToLowerInvariant();
        entity.Description = request.Description.Trim();
        entity.Location = request.Location.Trim();
        entity.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        entity.IsPublished = request.IsPublished;
        await repository.SaveChangesAsync(ct);
        return ToResponse(entity);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct) => repository.DeleteAsync(id, ct);

    private static DestinationResponse ToResponse(Destination d) =>
        new(d.Id, d.Name, d.Slug, d.Description, d.Location, d.ImageUrl, d.IsPublished);
}
