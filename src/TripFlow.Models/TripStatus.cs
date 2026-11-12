namespace TripFlow.Models;

/// <summary>Статус поїздки. Не зберігається в базі, а обчислюється за датами (див. ARCHITECTURE.md).</summary>
public enum TripStatus
{
    /// <summary>Поїздка ще не почалась.</summary>
    Planned,

    /// <summary>Поїздка триває: сьогодні між датою початку й датою завершення включно.</summary>
    InProgress,

    /// <summary>Поїздка завершилась.</summary>
    Completed
}
