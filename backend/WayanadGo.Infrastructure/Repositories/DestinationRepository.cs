using Microsoft.EntityFrameworkCore;
using WayanadGo.Application.Interfaces;
using WayanadGo.Infrastructure.Data;
using WayanadGo.Application.Entities;

namespace WayanadGo.Infrastructure.Repositories;

public sealed class DestinationRepository(AppDbContext db) : IDestinationRepository
{
    public async Task<IReadOnlyList<Destination>> GetPublishedAsync(string? search, CancellationToken ct)
    {
        var query = db.Destinations.AsNoTracking().Where(x => x.IsPublished);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{term}%") ||
                                     EF.Functions.ILike(x.Location, $"%{term}%") ||
                                     EF.Functions.ILike(x.Description, $"%{term}%"));
        }
        return await query.OrderBy(x => x.Name).Take(100).ToListAsync(ct);
    }

    public Task<Destination?> GetByIdAsync(int id, CancellationToken ct) =>
        db.Destinations.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Destination?> GetBySlugAsync(string slug, CancellationToken ct) =>
        db.Destinations.FirstOrDefaultAsync(x => x.Slug == slug, ct);

    public async Task<Destination> AddAsync(Destination destination, CancellationToken ct)
    {
        db.Destinations.Add(destination);
        await db.SaveChangesAsync(ct);
        return destination;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var entity = await db.Destinations.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;
        db.Destinations.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
