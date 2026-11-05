using System.Globalization;

namespace TripFlow.ViewModels;

/// <summary>Єдиний формат грошових сум на екранах: «1 400 ₴», «620,50 ₴».</summary>
public static class MoneyFormat
{
    private static readonly CultureInfo Ua = CultureInfo.GetCultureInfo("uk-UA");

    public static string Format(decimal amount) =>
        amount.ToString(amount % 1 == 0 ? "N0" : "N2", Ua) + " ₴";
}
