using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections.ObjectModel;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class PersonnelSettingsDialogViewModel : ViewModelBase
{
    private readonly IPersonnelAssignmentService _service;
    private string _newName = string.Empty;
    private string _renameText = string.Empty;
    private Personnel? _selected;

    public PersonnelSettingsDialogViewModel(IPersonnelAssignmentService service)
    {
        _service = service;
        Personnel = new ObservableCollection<Personnel>(_service.GetPersonnel());
        AddCommand = new AsyncRelayCommand(AddAsync, () => !string.IsNullOrWhiteSpace(NewName));
        RenameCommand = new AsyncRelayCommand(RenameAsync, () => Selected is not null && !string.IsNullOrWhiteSpace(RenameText));
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => Selected is not null);
        CloseCommand = new RelayCommand(() => RequestClose?.Invoke(this, true));
    }

    public event EventHandler<bool>? RequestClose;

    public ObservableCollection<Personnel> Personnel { get; }

    public string NewName
    {
        get => _newName;
        set
        {
            if (SetProperty(ref _newName, value))
            {
                AddCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string RenameText
    {
        get => _renameText;
        set
        {
            if (SetProperty(ref _renameText, value))
            {
                RenameCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public Personnel? Selected
    {
        get => _selected;
        set
        {
            if (SetProperty(ref _selected, value))
            {
                RenameText = value?.Name ?? string.Empty;
                RenameCommand?.NotifyCanExecuteChanged();
                DeleteCommand?.NotifyCanExecuteChanged();
            }
        }
    }

    public AsyncRelayCommand AddCommand { get; }
    public AsyncRelayCommand RenameCommand { get; }
    public AsyncRelayCommand DeleteCommand { get; }
    public RelayCommand CloseCommand { get; }

    private async Task AddAsync()
    {
        var person = await _service.AddPersonnelAsync(NewName);
        Personnel.Add(person);
        NewName = string.Empty;
        Selected = person;
    }

    private async Task RenameAsync()
    {
        if (Selected is null)
        {
            return;
        }

        await _service.RenamePersonnelAsync(Selected.Id, RenameText);
        Selected.Name = RenameText.Trim();
        Selected.UpdatedAt = DateTime.Now;
    }

    private async Task DeleteAsync()
    {
        if (Selected is null)
        {
            return;
        }

        var id = Selected.Id;
        await _service.DeletePersonnelAsync(id);
        Personnel.Remove(Selected);
        Selected = null;
    }
}
