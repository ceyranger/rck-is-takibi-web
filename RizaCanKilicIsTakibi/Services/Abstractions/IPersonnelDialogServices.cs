using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IPersonnelSettingsDialogService
{
    Task ShowDialogAsync(CancellationToken cancellationToken = default);
}

public interface IPersonnelPickDialogService
{
    Task<Guid?> ShowDialogAsync(CancellationToken cancellationToken = default);
}

public interface IPersonnelCellScopeDialogService
{
    PersonnelCellScopeChoice ShowDialog(string columnLabel);
}
