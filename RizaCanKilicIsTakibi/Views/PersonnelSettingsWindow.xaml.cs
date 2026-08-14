using RizaCanKilicIsTakibi.ViewModels;
using System.Windows;

namespace RizaCanKilicIsTakibi.Views;

public partial class PersonnelSettingsWindow : Window
{
    public PersonnelSettingsWindow(PersonnelSettingsDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += (_, _) =>
        {
            DialogResult = true;
            Close();
        };
    }
}
