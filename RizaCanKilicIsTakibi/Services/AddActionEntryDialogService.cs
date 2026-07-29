using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;
using RizaCanKilicIsTakibi.Views;
using System.Windows;

namespace RizaCanKilicIsTakibi.Services;

public sealed class AddActionEntryDialogService : IAddActionEntryDialogService
{
    private readonly IProjectCatalogUiState _catalogUiState;
    private readonly IProjectCatalogService _catalogService;

    public AddActionEntryDialogService(
        IProjectCatalogUiState catalogUiState,
        IProjectCatalogService catalogService)
    {
        _catalogUiState = catalogUiState;
        _catalogService = catalogService;
    }

    public Task<ActionEntry?> ShowDialogAsync(string district, ActionEntryCategory category, CancellationToken cancellationToken = default)
        => ShowDialogAsync(new AddActionEntryDialogRequest
        {
            District = district,
            Category = category
        }, cancellationToken);

    public Task<ActionEntry?> ShowDialogAsync(AddActionEntryDialogRequest request, CancellationToken cancellationToken = default)
    {
        var vm = new AddActionEntryDialogViewModel(
            request,
            _catalogUiState.GetActiveEntries(),
            _catalogService);
        var window = new AddActionEntryWindow(vm)
        {
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        };

        var result = window.ShowDialog();
        if (result == true)
        {
            return Task.FromResult<ActionEntry?>(vm.BuildEntry(0));
        }

        return Task.FromResult<ActionEntry?>(null);
    }
}
