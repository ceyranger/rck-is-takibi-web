using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;
using RizaCanKilicIsTakibi.Views;
using System.Windows;

namespace RizaCanKilicIsTakibi.Services;

public sealed class QuickTaskTemplateDialogService : IQuickTaskTemplateDialogService
{
    private readonly IQuickTaskTemplateRepository _repository;

    public QuickTaskTemplateDialogService(IQuickTaskTemplateRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<string>?> ShowDialogAsync(CancellationToken cancellationToken = default)
    {
        var templates = await _repository.GetAllAsync(cancellationToken);
        var vm = new QuickTaskTemplateDialogViewModel(_repository, templates);
        var window = new QuickTaskTemplateWindow(vm)
        {
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        };

        var result = window.ShowDialog();
        return result == true ? vm.SelectedTitles : null;
    }
}
