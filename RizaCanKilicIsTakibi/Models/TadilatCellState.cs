namespace RizaCanKilicIsTakibi.Models;

public sealed class TadilatCellState
{
    public Guid EntryId { get; set; }
    public string ColumnKey { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
    public string NoteText { get; set; } = string.Empty;
}
