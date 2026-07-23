using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;
using RizaCanKilicIsTakibi.Views;
using System.Windows;

namespace RizaCanKilicIsTakibi.Services;

public sealed class CrashRecoveryWizardService : ICrashRecoveryWizardService
{
    public CrashRecoveryWizardChoice? Show(CrashRecoveryWizardRequest request)
    {
        var viewModel = new CrashRecoveryWizardViewModel(request);
        var window = new CrashRecoveryWizardWindow(viewModel)
        {
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(item => item.IsActive)
                    ?? Application.Current?.MainWindow
        };

        var result = window.ShowDialog();
        if (result is null)
        {
            return CrashRecoveryWizardChoice.Discard;
        }

        return viewModel.Choice ?? CrashRecoveryWizardChoice.Discard;
    }
}
