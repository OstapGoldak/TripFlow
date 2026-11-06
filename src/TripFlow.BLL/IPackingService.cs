using TripFlow.Models;

namespace TripFlow.BLL;

public interface IPackingRepository
{
    Task<IReadOnlyList<PackingItem>> GetByTripAsync(int tripId, CancellationToken ct = default);

    /// <summary>Додає річ і повертає її новий Id.</summary>
    Task<int> AddAsync(PackingItem item, CancellationToken ct = default);

    Task SetPackedAsync(int id, bool isPacked, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}

public interface IPackingService
{
    Task<IReadOnlyList<PackingItem>> GetByTripAsync(int tripId, CancellationToken ct = default);

    Task<int> AddAsync(PackingItem item, CancellationToken ct = default);

    Task SetPackedAsync(int id, bool isPacked, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}