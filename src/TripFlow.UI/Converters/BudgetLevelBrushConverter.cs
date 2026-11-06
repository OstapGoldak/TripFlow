using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TripFlow.BLL;

namespace TripFlow.Converters;

/// <summary>Перетворює стан бюджету на колір: звичайний, попередження (від 80%) або перевитрата (червоний).</summary>
public class BudgetLevelBrushConverter : IValueConverter
{
    public Brush? NormalBrush { get; set; }
    public Brush? WarningBrush { get; set; }
    public Brush? OverBrush { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is BudgetLevel level
            ? level switch
            {
                BudgetLevel.Over => OverBrush,
                BudgetLevel.Warning => WarningBrush,
                _ => NormalBrush
            }
            : NormalBrush;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
