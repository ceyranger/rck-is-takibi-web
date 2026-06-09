namespace RizaCanKilicIsTakibi.Commands;

public sealed class DelegateUndoableAction : IUndoableAction
{
    private readonly Action _execute;
    private readonly Action _undo;

    public DelegateUndoableAction(string description, Action execute, Action undo)
    {
        Description = description;
        _execute = execute;
        _undo = undo;
    }

    public string Description { get; }

    public void Execute() => _execute();

    public void Undo() => _undo();
}
