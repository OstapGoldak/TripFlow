using Moq;
using TripFlow.BLL;
using TripFlow.Models;

namespace TripFlow.Tests;

/// <summary>
/// Модульні тести PackingService: валідація речі чек-листа та прогрес зборів.
/// Репозиторій замінено заглушкою (Moq), тому база даних не потрібна.
/// </summary>
public class PackingServiceTests
{
    private readonly Mock<IPackingRepository> _repository = new();
    private readonly PackingService _service;

    public PackingServiceTests()
    {
        _service = new PackingService(_repository.Object);
    }

    private static PackingItem ValidItem() => new()
    {
        TripId = 7,
        Name = "Паспорт",
        Category = "Documents"
    };

    private void VerifyNothingSaved() =>
        _repository.Verify(r => r.AddAsync(It.IsAny<PackingItem>(), It.IsAny<CancellationToken>()), Times.Never);

    // ---------- Додавання: успішні випадки ----------

    [Fact]
    public async Task AddAsync_ValidItem_SavesAndReturnsId()
    {
        _repository.Setup(r => r.AddAsync(It.IsAny<PackingItem>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(15);

        var result = await _service.AddAsync(ValidItem());

        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.Value);
        _repository.Verify(r => r.AddAsync(It.IsAny<PackingItem>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_TrimsName()
    {
        var item = ValidItem();
        item.Name = "  Зарядка  ";

        await _service.AddAsync(item);

        _repository.Verify(r => r.AddAsync(
            It.Is<PackingItem>(i => i.Name == "Зарядка"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Documents")]
    [InlineData("Clothing")]
    [InlineData("Electronics")]
    [InlineData("FirstAid")]
    [InlineData("Other")]
    public async Task AddAsync_EveryKnownCategory_IsAllowed(string category)
    {
        var item = ValidItem();
        item.Category = category;

        var result = await _service.AddAsync(item);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AddAsync_NameOfMaxLength_IsAllowed()
    {
        var item = ValidItem();
        item.Name = new string('а', 255);

        var result = await _service.AddAsync(item);

        Assert.True(result.IsSuccess);
    }

    // ---------- Помилки валідації: нічого не зберігається ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task AddAsync_EmptyName_ReturnsFailureAndDoesNotSave(string? name)
    {
        var item = ValidItem();
        item.Name = name!;

        var result = await _service.AddAsync(item);

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
        VerifyNothingSaved();
    }

    [Fact]
    public async Task AddAsync_NameLongerThanMax_ReturnsFailureAndDoesNotSave()
    {
        var item = ValidItem();
        item.Name = new string('а', 256);

        var result = await _service.AddAsync(item);

        Assert.False(result.IsSuccess);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task AddAsync_NameLongerThanMaxOnlyBecauseOfSpaces_IsAllowed()
    {
        // Довжина перевіряється після обрізання пробілів
        var item = ValidItem();
        item.Name = "  " + new string('а', 255) + "  ";

        var result = await _service.AddAsync(item);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("Food")]
    [InlineData("documents")] // регістр має значення: так само перевіряє CHECK у базі
    [InlineData("")]
    [InlineData(null)]
    public async Task AddAsync_UnknownCategory_ReturnsFailureAndDoesNotSave(string? category)
    {
        var item = ValidItem();
        item.Category = category!;

        var result = await _service.AddAsync(item);

        Assert.False(result.IsSuccess);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task AddAsync_NullItem_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddAsync(null!));
    }

    // ---------- Інші операції передаються в репозиторій ----------

    [Fact]
    public async Task SetPackedAsync_CallsRepository()
    {
        await _service.SetPackedAsync(3, true);

        _repository.Verify(r => r.SetPackedAsync(3, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CallsRepository()
    {
        await _service.DeleteAsync(3);

        _repository.Verify(r => r.DeleteAsync(3, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Прогрес зборів ----------

    [Theory]
    [InlineData(0, 0, 0)]     // порожній список: без ділення на нуль
    [InlineData(0, 4, 0)]     // нічого не спаковано
    [InlineData(1, 3, 33.33)] // неціле значення
    [InlineData(3, 4, 75)]
    [InlineData(4, 4, 100)]   // усе спаковано
    public void CalculateProgress_ReturnsPercent(int packed, int total, double expectedPercent)
    {
        var progress = PackingService.CalculateProgress(packed, total);

        Assert.Equal(expectedPercent, progress.Percent, 2);
    }

    [Fact]
    public void CalculateProgress_FromItems_CountsOnlyPackedOnes()
    {
        var items = new[]
        {
            new PackingItem { Name = "Паспорт", IsPacked = true },
            new PackingItem { Name = "Зарядка", IsPacked = true },
            new PackingItem { Name = "Аптечка", IsPacked = false },
            new PackingItem { Name = "Куртка", IsPacked = false }
        };

        var progress = PackingService.CalculateProgress(items);

        Assert.Equal(2, progress.Packed);
        Assert.Equal(4, progress.Total);
        Assert.Equal(50, progress.Percent, 2);
    }

    [Fact]
    public void CalculateProgress_FromEmptyList_IsZero()
    {
        var progress = PackingService.CalculateProgress(Array.Empty<PackingItem>());

        Assert.Equal(0, progress.Total);
        Assert.Equal(0, progress.Percent);
    }
}
