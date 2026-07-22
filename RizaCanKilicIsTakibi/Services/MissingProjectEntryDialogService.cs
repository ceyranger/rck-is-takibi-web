using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;
using RizaCanKilicIsTakibi.Views;
using System.Windows;

namespace RizaCanKilicIsTakibi.Services;

public sealed class MissingProjectEntryDialogService : IMissingProjectEntryDialogService
{
    private readonly IProjectCatalogUiState _catalogUiState;
    private readonly IProjectCatalogService _catalogService;

    public MissingProjectEntryDialogService(
        IProjectCatalogUiState catalogUiState,
        IProjectCatalogService catalogService)
    {
        _catalogUiState = catalogUiState;
        _catalogService = catalogService;
    }

    public Task<MissingProjectEntry?> ShowDialogAsync(CancellationToken cancellationToken = default)
    {
        var viewModel = new MissingProjectEntryDialogViewModel(
            _catalogUiState.GetActiveEntries(),
            _catalogService);
        var window = new MissingProjectEntryWindow(viewModel)
        {
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(item => item.IsActive)
        };

        MissingProjectEntry? result = null;
        viewModel.RequestClose += (_, entry) => result = entry;
        var showResult = window.ShowDialog();
        return Task.FromResult(showResult == true ? result : null);
    }
}
