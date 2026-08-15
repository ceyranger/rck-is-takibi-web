using System.Windows;
using RizaCanKilicIsTakibi.ViewModels;

namespace RizaCanKilicIsTakibi.Views;

public partial class PersonnelManualAssignmentWindow : Window
{
    public PersonnelManualAssignmentWindow(PersonnelManualAssignmentDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += (_, result) =>
        {
            DialogResult = result;
            Close();
        };
    }
}
