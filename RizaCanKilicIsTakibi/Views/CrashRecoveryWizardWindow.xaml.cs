using RizaCanKilicIsTakibi.ViewModels;
using System.Windows;

namespace RizaCanKilicIsTakibi.Views;

public partial class CrashRecoveryWizardWindow : Window
{
    public CrashRecoveryWizardWindow(CrashRecoveryWizardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += (_, _) =>
        {
            DialogResult = viewModel.Choice == Models.CrashRecoveryWizardChoice.Recover;
            Close();
        };
    }
}
