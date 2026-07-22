using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;
using RizaCanKilicIsTakibi.Views;
using System.Windows;

namespace RizaCanKilicIsTakibi.Services;

public sealed class ProjectLinkResolveDialogService : IProjectLinkResolveDialogService
{
    private readonly IProjectCatalogService _catalogService;
    private readonly IProjectCatalogEntryDialogService _catalogEntryDialogService;
    private readonly IConfirmationService _confirmationService;

    public ProjectLinkResolveDialogService(
        IProjectCatalogService catalogService,
        IProjectCatalogEntryDialogService catalogEntryDialogService,
        IConfirmationService confirmationService)
    {
        _catalogService = catalogService;
        _catalogEntryDialogService = catalogEntryDialogService;
        _confirmationService = confirmationService;
    }

    public Task<IReadOnlyList<UnresolvedLinkResolution>?> ShowDialogAsync(
        IReadOnlyList<UnresolvedProjectLinkItem> unresolved,
        IReadOnlyList<ProjectCatalogEntry> catalog,
        CancellationToken cancellationToken = default)
    {
        var vm = new ProjectLinkResolveDialogViewModel(
            unresolved,
            catalog,
            _catalogService,
            _catalogEntryDialogService,
            _confirmationService);
        var window = new ProjectLinkResolveWindow(vm)
        {
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        };

        var result = window.ShowDialog();
        if (result == true)
        {
            return Task.FromResult<IReadOnlyList<UnresolvedLinkResolution>?>(vm.BuildResolutions());
        }

        return Task.FromResult<IReadOnlyList<UnresolvedLinkResolution>?>(null);
    }
}
