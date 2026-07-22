using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;
using RizaCanKilicIsTakibi.Views;
using System.Windows;

namespace RizaCanKilicIsTakibi.Services;

public sealed class YibfAnaBilgiEntryDialogService : IYibfAnaBilgiEntryDialogService
{
    private readonly IProjectCatalogUiState _catalogUiState;
    private readonly IProjectCatalogService _catalogService;

    public YibfAnaBilgiEntryDialogService(
        IProjectCatalogUiState catalogUiState,
        IProjectCatalogService catalogService)
    {
        _catalogUiState = catalogUiState;
        _catalogService = catalogService;
    }

    public Task<YibfAnaBilgiEntryDialogResult?> ShowDialogAsync(
        YibfAnaBilgiEntryDialogResult? initialValues = null,
        bool isEditMode = false,
        CancellationToken cancellationToken = default)
    {
        var viewModel = new YibfAnaBilgiEntryDialogViewModel(
            _catalogUiState.GetActiveEntries(),
            _catalogService,
            initialValues,
            isEditMode);
        var window = new YibfAnaBilgiEntryWindow(viewModel)
        {
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(item => item.IsActive)
        };

        YibfAnaBilgiEntryDialogResult? result = null;
        viewModel.RequestClose += (_, dialogResult) => result = dialogResult;
        var showResult = window.ShowDialog();
        return Task.FromResult(showResult == true ? result : result);
    }
}
