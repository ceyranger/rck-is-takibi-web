using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using System.Collections.ObjectModel;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class CrashRecoveryWizardViewModel : ViewModelBase
{
    private CrashRecoveryWizardChoice? _choice;

    public CrashRecoveryWizardViewModel(CrashRecoveryWizardRequest request)
    {
        LastSuccessfulSaveText = request.LastSuccessfulSaveAt is DateTime lastSave
            ? lastSave.ToString("dd.MM.yyyy HH:mm:ss")
            : "Yok";
        RecoveryCreatedText = request.RecoveryCreatedAt is DateTime recoveryAt
            ? recoveryAt.ToString("dd.MM.yyyy HH:mm:ss")
            : "Bilinmiyor";
        ChangeLines = new ObservableCollection<string>(request.ChangeLines);

        RecoverCommand = new RelayCommand(() => CloseWith(CrashRecoveryWizardChoice.Recover));
        DiscardCommand = new RelayCommand(() => CloseWith(CrashRecoveryWizardChoice.Discard));
    }

    public event EventHandler? RequestClose;

    public string LastSuccessfulSaveText { get; }
    public string RecoveryCreatedText { get; }
    public ObservableCollection<string> ChangeLines { get; }
    public CrashRecoveryWizardChoice? Choice => _choice;

    public RelayCommand RecoverCommand { get; }
    public RelayCommand DiscardCommand { get; }

    private void CloseWith(CrashRecoveryWizardChoice choice)
    {
        _choice = choice;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}
