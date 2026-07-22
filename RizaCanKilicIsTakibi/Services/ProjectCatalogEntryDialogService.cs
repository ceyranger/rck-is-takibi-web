using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;
using RizaCanKilicIsTakibi.Views;
using System.Windows;

namespace RizaCanKilicIsTakibi.Services;

public sealed class ProjectCatalogEntryDialogService : IProjectCatalogEntryDialogService
{
    public Task<ProjectCatalogEntry?> ShowDialogAsync(
        ProjectCatalogEntry? existing,
        IReadOnlyList<ProjectCatalogEntry> catalog,
        CancellationToken cancellationToken = default)
    {
        var vm = new ProjectCatalogEntryDialogViewModel(existing, catalog);
        var window = new ProjectCatalogEntryWindow(vm)
        {
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        };

        var result = window.ShowDialog();
        if (result == true)
        {
            return Task.FromResult<ProjectCatalogEntry?>(vm.BuildEntry());
        }

        return Task.FromResult<ProjectCatalogEntry?>(null);
    }
}
