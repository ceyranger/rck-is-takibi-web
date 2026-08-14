using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class PersonnelCellScopeDialogViewModel : ViewModelBase
{
    public PersonnelCellScopeDialogViewModel(string columnLabel)
    {
        Message = $"“{columnLabel}” için atama kapsamı:";
        ThisCellCommand = new RelayCommand(() =>
        {
            Choice = Services.Abstractions.PersonnelCellScopeChoice.ThisCell;
            RequestClose?.Invoke(this, true);
        });
        AllRedYellowCommand = new RelayCommand(() =>
        {
            Choice = Services.Abstractions.PersonnelCellScopeChoice.AllRedYellowOnRow;
            RequestClose?.Invoke(this, true);
        });
        CancelCommand = new RelayCommand(() =>
        {
            Choice = Services.Abstractions.PersonnelCellScopeChoice.Cancel;
            RequestClose?.Invoke(this, false);
        });
    }

    public event EventHandler<bool>? RequestClose;

    public string Message { get; }
    public Services.Abstractions.PersonnelCellScopeChoice Choice { get; private set; } = Services.Abstractions.PersonnelCellScopeChoice.Cancel;
    public RelayCommand ThisCellCommand { get; }
    public RelayCommand AllRedYellowCommand { get; }
    public RelayCommand CancelCommand { get; }
}
