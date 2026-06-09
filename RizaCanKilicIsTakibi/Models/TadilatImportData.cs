namespace RizaCanKilicIsTakibi.Models;

public sealed class TadilatImportData
{
    public IReadOnlyList<TadilatEntry> Entries { get; init; } = Array.Empty<TadilatEntry>();
    public IReadOnlyList<TadilatCellState> CellStates { get; init; } = Array.Empty<TadilatCellState>();
}
