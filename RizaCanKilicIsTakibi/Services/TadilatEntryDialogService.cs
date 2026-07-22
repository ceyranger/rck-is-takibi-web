using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;
using RizaCanKilicIsTakibi.Views;
using System.Windows;

namespace RizaCanKilicIsTakibi.Services;

public sealed class TadilatEntryDialogService : ITadilatEntryDialogService
{
    private readonly IProjectCatalogUiState _catalogUiState;
    private readonly IProjectCatalogService _catalogService;

    public TadilatEntryDialogService(
        IProjectCatalogUiState catalogUiState,
        IProjectCatalogService catalogService)
    {
        _catalogUiState = catalogUiState;
        _catalogService = catalogService;
    }

    public Task<TadilatEntry?> ShowDialogAsync(string district, TadilatSubTab subTab, CancellationToken cancellationToken = default)
    {
        var viewModel = new TadilatEntryDialogViewModel(
            district,
            subTab,
            _catalogUiState.GetActiveEntries(),
            _catalogService);
        var window = new TadilatEntryWindow(viewModel)
        {
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(item => item.IsActive)
        };

        TadilatEntry? result = null;
        viewModel.RequestClose += (_, entry) => result = entry;
        var showResult = window.ShowDialog();
        return Task.FromResult(showResult == true ? result : null);
    }
}
