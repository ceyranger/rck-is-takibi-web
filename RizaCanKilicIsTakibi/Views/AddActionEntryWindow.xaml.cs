using RizaCanKilicIsTakibi.ViewModels;
using System.Windows;

namespace RizaCanKilicIsTakibi.Views;

public partial class AddActionEntryWindow : Window
{
    public AddActionEntryWindow(AddActionEntryDialogViewModel viewModel)
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
