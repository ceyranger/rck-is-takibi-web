namespace RizaCanKilicIsTakibi.Models;

public sealed class AddActionEntryDialogRequest
{
    public string District { get; init; } = string.Empty;
    public IReadOnlyList<string> DistrictOptions { get; init; } = [];
    public ActionEntryCategory Category { get; init; }
    public string OwnerParcelText { get; init; } = string.Empty;
    public string WorkText { get; init; } = string.Empty;
    public Guid? ProjectId { get; init; }
}
