using System.Globalization;

namespace TripFlow.ViewModels;

internal static class Parsing
{
    /// <summary>Розбирає суму з поточною культурою, а також з крапкою як роздільником.</summary>
    public static bool TryDecimal(string? text, out decimal value)
    {
        var normalized = (text ?? string.Empty).Trim().Replace('\u00A0', ' ');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
               || decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
