using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;
using RizaCanKilicIsTakibi.Views;
using System.Windows;

namespace RizaCanKilicIsTakibi.Services;

public sealed class PersonnelSettingsDialogService : IPersonnelSettingsDialogService
{
    private readonly IPersonnelAssignmentService _service;

    public PersonnelSettingsDialogService(IPersonnelAssignmentService service)
    {
        _service = service;
    }

    public Task ShowDialogAsync(CancellationToken cancellationToken = default)
    {
        var vm = new PersonnelSettingsDialogViewModel(_service);
        var window = new PersonnelSettingsWindow(vm)
        {
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current?.MainWindow
        };
        window.ShowDialog();
        return Task.CompletedTask;
    }
}

public sealed class PersonnelPickDialogService : IPersonnelPickDialogService
{
    private readonly IPersonnelAssignmentService _service;

    public PersonnelPickDialogService(IPersonnelAssignmentService service)
    {
        _service = service;
    }

    public Task<Guid?> ShowDialogAsync(CancellationToken cancellationToken = default)
    {
        var people = _service.GetPersonnel();
        if (people.Count == 0)
        {
            MessageBox.Show(
                "Önce Personel Görev Takibi → Personel Ayarları ile personel ekleyin.",
                "Personel yok",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return Task.FromResult<Guid?>(null);
        }

        var vm = new PersonnelPickDialogViewModel(people);
        var window = new PersonnelPickWindow(vm)
        {
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current?.MainWindow
        };
        var result = window.ShowDialog();
        return Task.FromResult(result == true ? vm.Selected?.Id : null);
    }
}

public sealed class PersonnelCellScopeDialogService : IPersonnelCellScopeDialogService
{
    public PersonnelCellScopeChoice ShowDialog(string columnLabel)
    {
        var vm = new PersonnelCellScopeDialogViewModel(columnLabel);
        var window = new PersonnelCellScopeWindow(vm)
        {
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current?.MainWindow
        };
        var result = window.ShowDialog();
        return result == true ? vm.Choice : PersonnelCellScopeChoice.Cancel;
    }
}

public sealed class PersonnelAssignmentEditDialogService : IPersonnelAssignmentEditDialogService
{
    private readonly IPersonnelAssignmentService _service;

    public PersonnelAssignmentEditDialogService(IPersonnelAssignmentService service)
    {
        _service = service;
    }

    public async Task<bool> ShowDialogAsync(PersonnelAssignment assignment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        var vm = new PersonnelAssignmentEditDialogViewModel(assignment, _service.GetPersonnel());
        var window = new PersonnelAssignmentEditWindow(vm)
        {
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current?.MainWindow
        };

        var result = window.ShowDialog();
        if (result != true)
        {
            return false;
        }

        await _service.UpdateAssignmentAsync(vm.BuildUpdatedAssignment(), cancellationToken);
        return true;
    }
}

public sealed class PersonnelManualAssignmentDialogService : IPersonnelManualAssignmentDialogService
{
    private readonly IPersonnelAssignmentService _service;

    public PersonnelManualAssignmentDialogService(IPersonnelAssignmentService service)
    {
        _service = service;
    }

    public async Task<bool> ShowCreateDialogAsync(CancellationToken cancellationToken = default)
    {
        var people = _service.GetPersonnel();
        if (people.Count == 0)
        {
            MessageBox.Show(
                "Önce Personel Ayarları ile personel ekleyin.",
                "Personel yok",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return false;
        }

        var vm = new PersonnelManualAssignmentDialogViewModel(people);
        var window = new PersonnelManualAssignmentWindow(vm)
        {
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current?.MainWindow
        };

        var result = window.ShowDialog();
        if (result != true)
        {
            return false;
        }

        await _service.AssignAsync(vm.BuildAssignment(), cancellationToken);
        return true;
    }
}
