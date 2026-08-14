using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections.ObjectModel;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class PersonnelPickDialogViewModel : ViewModelBase
{
    private Personnel? _selected;

    public PersonnelPickDialogViewModel(IEnumerable<Personnel> personnel)
    {
        Personnel = new ObservableCollection<Personnel>(personnel);
        Selected = Personnel.FirstOrDefault();
        OkCommand = new RelayCommand(() => RequestClose?.Invoke(this, true), () => Selected is not null);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public ObservableCollection<Personnel> Personnel { get; }

    public Personnel? Selected
    {
        get => _selected;
        set
        {
            if (SetProperty(ref _selected, value))
            {
                OkCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public RelayCommand OkCommand { get; }
    public RelayCommand CancelCommand { get; }
}
