using RizaCanKilicIsTakibi.Commands;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IUndoRedoService
{
    bool CanUndo { get; }
    bool CanRedo { get; }
    event EventHandler? StateChanged;

    void Execute(IUndoableAction action);
    void Undo();
    void Redo();
    void Clear();
}
