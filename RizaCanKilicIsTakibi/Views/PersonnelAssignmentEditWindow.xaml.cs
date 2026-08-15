using System.Windows;
using RizaCanKilicIsTakibi.ViewModels;

namespace RizaCanKilicIsTakibi.Views;

public partial class PersonnelAssignmentEditWindow : Window
{
    public PersonnelAssignmentEditWindow(PersonnelAssignmentEditDialogViewModel viewModel)
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
