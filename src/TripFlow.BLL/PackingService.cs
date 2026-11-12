using TripFlow.Models;

namespace TripFlow.BLL;

/// <summary>Бізнес-правила чек-листа: перевірка назви та категорії перед записом у базу.</summary>
public class PackingService : IPackingService
{
    private readonly IPackingRepository _repository;

    public PackingService(IPackingRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<PackingItem>> GetByTripAsync(int tripId, CancellationToken ct = default) =>
        _repository.GetByTripAsync(tripId, ct);

    public async Task<OperationResult<int>> AddAsync(PackingItem item, CancellationToken ct = default)
{
    ArgumentNullException.ThrowIfNull(item);

    item.Name = (item.Name ?? string.Empty).Trim();
    if (item.Name.Length == 0)
    {
        return OperationResult<int>.Failure("Назва речі не може бути порожньою.");
    }

    if (item.Name.Length > 255)
    {
        return OperationResult<int>.Failure("Назва речі задовга (максимум 255 символів).");
    }

    if (!PackingCategories.IsValid(item.Category))
    {
        return OperationResult<int>.Failure("Невідома категорія речі.");
    }

    var id = await _repository.AddAsync(item, ct);
    return OperationResult<int>.Success(id);
}

    public Task SetPackedAsync(int id, bool isPacked, CancellationToken ct = default) =>
        _repository.SetPackedAsync(id, isPacked, ct);

    public Task DeleteAsync(int id, CancellationToken ct = default) =>
        _repository.DeleteAsync(id, ct);
}