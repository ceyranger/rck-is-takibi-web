using RizaCanKilicIsTakibi.Commands;
using RizaCanKilicIsTakibi.Services.Abstractions;

namespace RizaCanKilicIsTakibi.Services;

public sealed class UndoRedoService : IUndoRedoService
{
    private readonly List<IUndoableAction> _undoStack = [];
    private readonly List<IUndoableAction> _redoStack = [];
    private readonly int _maxHistory;

    public UndoRedoService(int maxHistory = 250)
    {
        _maxHistory = Math.Max(1, maxHistory);
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public event EventHandler? StateChanged;

    public void Execute(IUndoableAction action)
    {
        action.Execute();
        _undoStack.Add(action);
        TrimHistory(_undoStack);
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Undo()
    {
        if (!CanUndo)
        {
            return;
        }

        var index = _undoStack.Count - 1;
        var action = _undoStack[index];
        _undoStack.RemoveAt(index);
        action.Undo();
        _redoStack.Add(action);
        TrimHistory(_redoStack);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Redo()
    {
        if (!CanRedo)
        {
            return;
        }

        var index = _redoStack.Count - 1;
        var action = _redoStack[index];
        _redoStack.RemoveAt(index);
        action.Execute();
        _undoStack.Add(action);
        TrimHistory(_undoStack);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TrimHistory(List<IUndoableAction> stack)
    {
        if (stack.Count <= _maxHistory)
        {
            return;
        }

        stack.RemoveRange(0, stack.Count - _maxHistory);
    }
}
