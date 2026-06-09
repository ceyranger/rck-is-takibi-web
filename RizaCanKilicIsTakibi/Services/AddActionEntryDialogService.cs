using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;
using RizaCanKilicIsTakibi.Views;
using System.Windows;

namespace RizaCanKilicIsTakibi.Services;

public sealed class AddActionEntryDialogService : IAddActionEntryDialogService
{
    public Task<ActionEntry?> ShowDialogAsync(string district, ActionEntryCategory category, CancellationToken cancellationToken = default)
    {
        var vm = new AddActionEntryDialogViewModel(district, category);
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
