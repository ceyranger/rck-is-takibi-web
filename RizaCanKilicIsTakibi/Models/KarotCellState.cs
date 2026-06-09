namespace RizaCanKilicIsTakibi.Models;

public sealed class KarotCellState
{
    public Guid EntryId { get; set; }
    public string ColumnKey { get; set; } = string.Empty;
    public string NoteText { get; set; } = string.Empty;
}
