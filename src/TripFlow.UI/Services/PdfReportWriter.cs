using System.Printing;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using TripFlow.BLL;
using TripFlow.ViewModels;

namespace TripFlow.Services;

/// <summary>
/// Малює звіт поїздки як документ WPF і друкує його на вбудований принтер Windows
/// «Microsoft Print to PDF». Сторонні бібліотеки не потрібні, кирилиця відображається
/// системним шрифтом. Місце збереження файлу Windows запитує у власному вікні.
/// </summary>
public static class PdfReportWriter
{
    private const string PdfPrinterName = "Microsoft Print to PDF";

    // A4 у одиницях WPF (1/96 дюйма)
    private const double PageWidth = 793.7;
    private const double PageHeight = 1122.5;

    private static readonly Brush Text = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));
    private static readonly Brush Dim = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
    private static readonly Brush Line = new SolidColorBrush(Color.FromRgb(0xD3, 0xE8, 0xFB));
    private static readonly Brush HeaderFill = new SolidColorBrush(Color.FromRgb(0xF3, 0xFA, 0xFF));

    /// <summary>Чи є в системі принтер «Microsoft Print to PDF».</summary>
    public static bool IsAvailable() => FindPdfPrinter() is not null;

    /// <summary>
    /// Друкує звіт у PDF. Повертає false, якщо принтера «Microsoft Print to PDF» немає.
    /// Якщо користувач закриє вікно збереження, Windows скасує завдання друку.
    /// </summary>
    public static bool Write(TripReport report)
    {
        var queue = FindPdfPrinter();
        if (queue is null)
        {
            return false;
        }

        var ticket = queue.DefaultPrintTicket?.Clone() ?? new PrintTicket();
        ticket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4);
        ticket.PageOrientation = PageOrientation.Portrait;

        var document = Build(report);
        var writer = PrintQueue.CreateXpsDocumentWriter(queue);
        writer.Write(((IDocumentPaginatorSource)document).DocumentPaginator, ticket);
        return true;
    }

    private static PrintQueue? FindPdfPrinter()
    {
        var server = new LocalPrintServer();
        var queues = server.GetPrintQueues().ToList();
        return queues.FirstOrDefault(q => q.Name == PdfPrinterName)
               ?? queues.FirstOrDefault(q => q.Name.Contains("PDF", StringComparison.OrdinalIgnoreCase));
    }

    // ---------- Вміст звіту ----------

    private static FlowDocument Build(TripReport report)
    {
        var trip = report.Trip;
        var document = new FlowDocument
        {
            PageWidth = PageWidth,
            PageHeight = PageHeight,
            PagePadding = new Thickness(56),
            ColumnWidth = double.PositiveInfinity, // одна колонка на всю ширину
            FontFamily = new FontFamily("Segoe UI, Segoe UI Symbol"),
            FontSize = 12,
            Foreground = Text
        };

        document.Blocks.Add(new Paragraph(new Run(trip.Name)) { FontSize = 24, FontWeight = FontWeights.Bold, Margin = new Thickness(0) });
        document.Blocks.Add(Dimmed($"{trip.Destination} · {trip.StartDate:dd.MM.yyyy} – {trip.EndDate:dd.MM.yyyy}"));
        document.Blocks.Add(Dimmed($"Звіт сформовано {DateTime.Now:dd.MM.yyyy HH:mm}"));

        AddSchedule(document, report);
        AddBudget(document, report.Budget);
        AddPacking(document, report);
        return document;
    }

    private static void AddSchedule(FlowDocument document, TripReport report)
    {
        document.Blocks.Add(Heading("Розклад"));
        foreach (var day in report.Days)
        {
            document.Blocks.Add(new Paragraph(new Run($"День {day.Number} · {day.Date:dd.MM.yyyy}"))
            {
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 10, 0, 4),
                KeepWithNext = true
            });

            if (day.Activities.Count == 0)
            {
                document.Blocks.Add(Dimmed("Подій немає"));
                continue;
            }

            var table = NewTable(1, 6, 2);
            foreach (var a in day.Activities)
            {
                var details = new List<string>();
                if (!string.IsNullOrWhiteSpace(a.Location)) details.Add("Локація: " + a.Location);
                if (!string.IsNullOrWhiteSpace(a.Note)) details.Add(a.Note!);
                var title = details.Count == 0 ? a.Title : $"{a.Title}\n{string.Join(" · ", details)}";
                var cost = a.EstimatedCost is { } c ? MoneyFormat.Format(c) : "—";
                AddRow(table, false, a.Time.ToString("HH:mm"), title, cost);
            }

            document.Blocks.Add(table);
        }
    }

    private static void AddBudget(FlowDocument document, BudgetOverview budget)
    {
        var s = budget.Summary;
        document.Blocks.Add(Heading("Бюджет"));

        var summary = NewTable(1, 1, 1, 1);
        AddRow(summary, true, "Плановий бюджет", "Фактично витрачено", "Залишок", "Використано ліміту");
        AddRow(summary, false, MoneyFormat.Format(s.Planned), MoneyFormat.Format(s.Spent),
               MoneyFormat.Format(s.Remaining), $"{Math.Round(s.UsedPercent):0}%");
        document.Blocks.Add(summary);

        if (s.Level == BudgetLevel.Over)
        {
            document.Blocks.Add(new Paragraph(new Run("Бюджет перевищено."))
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x7D)),
                Margin = new Thickness(0, 6, 0, 0)
            });
        }

        document.Blocks.Add(new Paragraph(new Run("Витрати")) { FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 14, 0, 4), KeepWithNext = true });
        if (budget.Expenses.Count == 0)
        {
            document.Blocks.Add(Dimmed("Витрат ще немає"));
            return;
        }

        var expenses = NewTable(2, 2, 4, 2);
        AddRow(expenses, true, "Дата", "Категорія", "Опис", "Сума");
        foreach (var e in budget.Expenses)
        {
            AddRow(expenses, false, e.Date.ToString("dd.MM.yyyy"), ExpenseCategoryNames.Text(e.Category),
                   e.Description ?? "", MoneyFormat.Format(e.Amount));
        }

        document.Blocks.Add(expenses);
    }

    private static void AddPacking(FlowDocument document, TripReport report)
    {
        document.Blocks.Add(Heading("Чек-лист"));
        document.Blocks.Add(Dimmed($"Спаковано {report.Packing.Packed} з {report.Packing.Total} ({Math.Round(report.Packing.Percent):0}%)"));

        if (report.PackingGroups.Count == 0)
        {
            document.Blocks.Add(Dimmed("Речей ще немає"));
            return;
        }

        foreach (var group in report.PackingGroups)
        {
            var packed = group.Items.Count(i => i.IsPacked);
            document.Blocks.Add(new Paragraph(new Run($"{group.Title} · {packed} з {group.Items.Count}"))
            {
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 10, 0, 4),
                KeepWithNext = true
            });

            var list = new List { MarkerStyle = TextMarkerStyle.None, Margin = new Thickness(0), Padding = new Thickness(0) };
            foreach (var item in group.Items)
            {
                var run = new Run((item.IsPacked ? "☑  " : "☐  ") + item.Name);
                if (item.IsPacked)
                {
                    run.Foreground = Dim;
                }

                list.ListItems.Add(new ListItem(new Paragraph(run) { Margin = new Thickness(0, 2, 0, 2) }));
            }

            document.Blocks.Add(list);
        }
    }

    // ---------- Допоміжні елементи ----------

    private static Paragraph Heading(string text) =>
        new(new Run(text))
        {
            FontSize = 17,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 22, 0, 4),
            KeepWithNext = true
        };

    private static Paragraph Dimmed(string text) =>
        new(new Run(text)) { Foreground = Dim, Margin = new Thickness(0, 2, 0, 0) };

    private static Table NewTable(params double[] columnWeights)
    {
        var table = new Table { CellSpacing = 0, BorderBrush = Line, BorderThickness = new Thickness(1) };
        foreach (var weight in columnWeights)
        {
            table.Columns.Add(new TableColumn { Width = new GridLength(weight, GridUnitType.Star) });
        }

        table.RowGroups.Add(new TableRowGroup());
        return table;
    }

    private static void AddRow(Table table, bool header, params string[] cells)
    {
        var row = new TableRow();
        if (header)
        {
            row.Background = HeaderFill;
            row.FontWeight = FontWeights.SemiBold;
        }

        foreach (var text in cells)
        {
            row.Cells.Add(new TableCell(new Paragraph(new Run(text)) { Margin = new Thickness(0) })
            {
                Padding = new Thickness(8, 5, 8, 5),
                BorderBrush = Line,
                BorderThickness = new Thickness(0, 0, 0, 1)
            });
        }

        table.RowGroups[0].Rows.Add(row);
    }
}
