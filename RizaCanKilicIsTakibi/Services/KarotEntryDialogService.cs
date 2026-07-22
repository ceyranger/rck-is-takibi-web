using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;
using RizaCanKilicIsTakibi.Views;
using System.Windows;

namespace RizaCanKilicIsTakibi.Services;

public sealed class KarotEntryDialogService : IKarotEntryDialogService
{
    private readonly IProjectCatalogUiState _catalogUiState;
    private readonly IProjectCatalogService _catalogService;

    public KarotEntryDialogService(
        IProjectCatalogUiState catalogUiState,
        IProjectCatalogService catalogService)
    {
        _catalogUiState = catalogUiState;
        _catalogService = catalogService;
    }

    public Task<KarotEntry?> ShowDialogAsync(KarotSubTab subTab, CancellationToken cancellationToken = default)
    {
        var viewModel = new KarotEntryDialogViewModel(
            subTab,
            _catalogUiState.GetActiveEntries(),
            _catalogService);
        var window = new KarotEntryWindow(viewModel)
        {
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(item => item.IsActive)
        };

        KarotEntry? result = null;
        viewModel.RequestClose += (_, entry) => result = entry;
        var showResult = window.ShowDialog();
        return Task.FromResult(showResult == true ? result : null);
    }
}
