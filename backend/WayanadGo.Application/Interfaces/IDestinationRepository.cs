using WayanadGo.Application.Entities;

namespace WayanadGo.Application.Interfaces;

public interface IDestinationRepository
{
    Task<IReadOnlyList<Destination>> GetPublishedAsync(string? search, CancellationToken cancellationToken);
    Task<Destination?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Destination?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<Destination> AddAsync(Destination destination, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
