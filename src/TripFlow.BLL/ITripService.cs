using TripFlow.Models;

namespace TripFlow.BLL;

/// <summary>Те, що ViewModel знає про поїздки. Про базу даних і SQL ViewModel не знає нічого.</summary>
public interface ITripService
{
    Task<IReadOnlyList<Trip>> GetAllAsync(CancellationToken ct = default);

    Task<int> AddAsync(Trip trip, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}
