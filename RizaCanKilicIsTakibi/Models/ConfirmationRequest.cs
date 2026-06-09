namespace RizaCanKilicIsTakibi.Models;

public sealed class ConfirmationRequest
{
    public required ConfirmationKind Kind { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public bool IsDestructive { get; init; }
}
