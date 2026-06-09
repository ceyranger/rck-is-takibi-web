using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;
using RizaCanKilicIsTakibi.Views;
using System.Windows;

namespace RizaCanKilicIsTakibi.Services;

public sealed class TadilatCellNoteDialogService : ITadilatCellNoteDialogService
{
    public Task<TadilatCellNoteDialogResult?> ShowDialogAsync(string currentNote, CancellationToken cancellationToken = default)
    {
        var viewModel = new TadilatCellNoteDialogViewModel(currentNote);
        var window = new TadilatCellNoteWindow(viewModel)
        {
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(item => item.IsActive)
        };

        TadilatCellNoteDialogResult? result = null;
        viewModel.RequestClose += (_, dialogResult) => result = dialogResult;
        var showResult = window.ShowDialog();
        return Task.FromResult(showResult == true ? result : result);
    }
}
