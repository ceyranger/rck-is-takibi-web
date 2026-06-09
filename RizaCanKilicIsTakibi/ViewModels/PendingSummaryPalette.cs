using System.Windows.Media;

namespace RizaCanKilicIsTakibi.ViewModels;

internal static class PendingSummaryPalette
{
    private static readonly BrushConverter BrushConverter = new();

    public static Brush UrgentBrush { get; } = CreateBrush("#FFD64545");
    public static Brush WarningBrush { get; } = CreateBrush("#FFFFEB3B");
    public static Brush LabelForegroundBrush { get; } = CreateBrush("#FF111111");

    public static Brush GetPriorityBrush(int priorityRank)
        => priorityRank == 0 ? UrgentBrush : WarningBrush;

    private static Brush CreateBrush(string color)
        => BrushConverter.ConvertFromString(color) as Brush ?? Brushes.Transparent;
}
