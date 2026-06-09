using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;
using RizaCanKilicIsTakibi.Views;
using System.Windows;

namespace RizaCanKilicIsTakibi.Services;

public sealed class KarotStatusDialogService : IKarotStatusDialogService
{
    public Task<KarotStatus?> ShowDialogAsync(KarotStatus currentStatus, CancellationToken cancellationToken = default)
    {
        var vm = new KarotStatusDialogViewModel(currentStatus);
        var window = new KarotStatusWindow(vm)
        {
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(item => item.IsActive)
        };

        var result = window.ShowDialog();
        return Task.FromResult(result == true ? (KarotStatus?)vm.SelectedStatus : null);
    }
}
