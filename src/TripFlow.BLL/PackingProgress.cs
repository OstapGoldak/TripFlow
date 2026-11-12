namespace TripFlow.BLL;

/// <summary>Прогрес зборів: скільки речей спаковано з усіх.</summary>
public sealed record PackingProgress(int Packed, int Total)
{
    /// <summary>Відсоток 0–100. Для порожнього списку 0 (без ділення на нуль).</summary>
    public double Percent => Total == 0 ? 0 : (double)Packed / Total * 100;
}