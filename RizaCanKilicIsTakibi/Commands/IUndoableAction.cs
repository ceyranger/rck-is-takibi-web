namespace RizaCanKilicIsTakibi.Commands;

public interface IUndoableAction
{
    string Description { get; }
    void Execute();
    void Undo();
}
