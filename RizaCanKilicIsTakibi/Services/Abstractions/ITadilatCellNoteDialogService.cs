using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface ITadilatCellNoteDialogService
{
    Task<TadilatCellNoteDialogResult?> ShowDialogAsync(string currentNote, CancellationToken cancellationToken = default);
}
