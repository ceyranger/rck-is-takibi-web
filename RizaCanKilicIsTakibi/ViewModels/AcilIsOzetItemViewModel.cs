namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class AcilIsOzetItemViewModel
{
    public required string Category { get; init; }
    public required string PriorityLabel { get; init; }
    public required int PriorityRank { get; init; }
    public required string Summary { get; init; }
    public required DateTime SourceUpdatedAt { get; init; }
    public System.Windows.Media.Brush PriorityBrush => PendingSummaryPalette.GetPriorityBrush(PriorityRank);
    public System.Windows.Media.Brush PriorityForegroundBrush => PendingSummaryPalette.LabelForegroundBrush;
}
