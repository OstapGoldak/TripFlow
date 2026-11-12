using TripFlow.Models;

namespace TripFlow.BLL;


public interface IPackingService
{
    Task<IReadOnlyList<PackingItem>> GetByTripAsync(int tripId, CancellationToken ct = default);

    Task<OperationResult<int>> AddAsync(PackingItem item, CancellationToken ct = default);

    Task SetPackedAsync(int id, bool isPacked, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}