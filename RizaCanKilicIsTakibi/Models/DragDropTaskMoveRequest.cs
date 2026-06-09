namespace RizaCanKilicIsTakibi.Models;

public sealed class DragDropTaskMoveRequest
{
    public required TaskItem Task { get; init; }
    public required TaskBoardType TargetBoard { get; init; }
}
