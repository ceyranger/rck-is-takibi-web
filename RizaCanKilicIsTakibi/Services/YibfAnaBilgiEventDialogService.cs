using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;
using RizaCanKilicIsTakibi.Views;
using System.Windows;

namespace RizaCanKilicIsTakibi.Services;

public sealed class YibfAnaBilgiEventDialogService : IYibfAnaBilgiEventDialogService
{
    public Task<YibfAnaBilgiEventDialogResult?> ShowDialogAsync(
        DateTime? eventDate,
        string description,
        string backgroundColor,
        string noteText,
        string approvalStatus = "",
        CancellationToken cancellationToken = default)
    {
        var viewModel = new YibfAnaBilgiEventDialogViewModel(eventDate, description, backgroundColor, noteText, approvalStatus);
        var window = new YibfAnaBilgiEventWindow(viewModel)
        {
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(item => item.IsActive)
        };

        YibfAnaBilgiEventDialogResult? result = null;
        viewModel.RequestClose += (_, dialogResult) => result = dialogResult;
        var showResult = window.ShowDialog();
        return Task.FromResult(showResult == true ? result : result);
    }
}
