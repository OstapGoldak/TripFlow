using TripFlow.Models;

namespace TripFlow.BLL;

/// <summary>
/// Walking Skeleton: поки лише передає виклики репозиторію.
/// Далі тут з'являться валідація (EndDate ≥ StartDate, Budget ≥ 0), статус за датами та фільтри.
/// </summary>
public class TripService : ITripService
{
    private readonly ITripRepository _repository;

    public TripService(ITripRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<Trip>> GetAllAsync(CancellationToken ct = default) =>
        _repository.GetAllAsync(ct);

    public Task<int> AddAsync(Trip trip, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(trip);
        return _repository.AddAsync(trip, ct);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default) =>
        _repository.DeleteAsync(id, ct);
}
